using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using MissingAssetsFinder.Lib;
using Mutagen.Bethesda;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MissingAssetsFinder
{
    public class MainWindowVM : ViewModel
    {
        private readonly MainWindow _mainWindow;

        [Reactive] public string SelectedDataPath { get; set; } = string.Empty;

        [Reactive] public List<string> SelectedPlugins { get; set; }

        [Reactive] public bool IsWorking { get; set; }

        [Reactive] public List<MissingAsset> MissingAssets { get; set; }

        [Reactive] public bool UseLoadOrder { get; set; }

        public ReactiveCommand<Unit, Unit> SelectDataFolder;
        public ReactiveCommand<Unit, Unit> SelectPlugins;
        public ReactiveCommand<Unit, Unit> Start;
        public ReactiveCommand<Unit, Unit> ViewResults;

        public ObservableCollectionExtended<StatusMessage> Log { get; } = new ObservableCollectionExtended<StatusMessage>();

        public MainWindowVM(MainWindow mainWindow)
        {
            Mutagen.Bethesda.Warmup.Init();

            _mainWindow = mainWindow;
            Utils.LogMessages
                .ObserveOn(RxApp.TaskpoolScheduler)
                .ToObservableChangeSet()
                .Buffer(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .Where(x => x.Count > 0)
                .FlattenBufferResult()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(Log)
                .Subscribe()
                .DisposeWith(CompositeDisposable);

            Utils.Log("Finished Logger setup");

            SelectedPlugins = new List<string>();
            MissingAssets = new List<MissingAsset>();

            SelectDataFolder = ReactiveCommand.Create(() =>
            {
                var dialog = new CommonOpenFileDialog
                {
                    Title = "Select your Data Folder",
                    IsFolderPicker = true,
                    Multiselect = false,
                    AddToMostRecentlyUsedList = false,
                    EnsurePathExists = true,
                    EnsureValidNames = true,
                };
                if (dialog.ShowDialog(_mainWindow) != CommonFileDialogResult.Ok) return;
                Utils.Log($"Selected: {dialog.FileName}");
                SelectedDataPath = dialog.FileName;
            }, this.WhenAny(x => x.IsWorking).Select(x => !x));

            SelectPlugins = ReactiveCommand.Create(() =>
            {
                var dialog = new CommonOpenFileDialog
                {
                    Title = "Select Plugins",
                    IsFolderPicker = false,
                    Multiselect = true,
                    AddToMostRecentlyUsedList = false,
                    EnsureFileExists = true,
                    EnsurePathExists = true,
                    EnsureValidNames = true,
                };
                dialog.Filters.Add(new CommonFileDialogFilter("Plugin", ".esm,.esp"));

                if (dialog.ShowDialog(_mainWindow) != CommonFileDialogResult.Ok) return;
                Utils.Log($"Selected {dialog.FileNames.Count()} files");
                SelectedPlugins = dialog.FileNames.ToList();
            }, this.WhenAny(x => x.IsWorking).CombineLatest(this.WhenAny(x => x.UseLoadOrder), (isWorking, useLoadOrder) => !isWorking && !useLoadOrder));

            Start = ReactiveCommand.CreateFromTask(FindMissingAssets,
                this.WhenAny(x => x.IsWorking).CombineLatest(
                    this.WhenAny(x => x.SelectedDataPath).Select(x => x.IsEmpty()),
                    this.WhenAny(x => x.SelectedPlugins).Select(x => x.All(y => y.IsEmpty())),
                        this.WhenAny(x => x.UseLoadOrder),
                    (isWorking, dataPathEmpty, pluginPathEmpty, useLoadOrder) => !isWorking && !dataPathEmpty && (!pluginPathEmpty || useLoadOrder)));

            ViewResults = ReactiveCommand.Create(() =>
                {
                    var resultsWindow = new ResultsWindow(MissingAssets);
                    _mainWindow.Closing += (sender, args) =>
                    {
                        resultsWindow.Close();
                    };
                    resultsWindow.Show();
                },
                this.WhenAny(x => x.IsWorking).CombineLatest(this.WhenAny(x => x.MissingAssets).Select(x => x.Count),
                    (isWorking, missingArchivesCount) => !isWorking && missingArchivesCount > 0));
        }

        public class FormKeyComparer : Comparer<FormKey>
        {
            public override int Compare(FormKey x, FormKey y)
            {
                if (string.Compare(x.ModKey.FileName, y.ModKey.FileName, StringComparison.Ordinal) != 0)
                {
                    return string.Compare(x.ModKey.FileName, y.ModKey.FileName, StringComparison.Ordinal);
                }

                if (x.ID.CompareTo(y.ID) != 0)
                {
                    return x.ID.CompareTo(y.ID);
                }

                return 0;
            }
        }

        public async Task FindMissingAssets(CancellationToken token)
        {
            IsWorking = true;

            var missingAssets = await Task.Run(async () =>
            {
                using var finder = new Finder(SelectedDataPath);

                await finder.BuildBSACacheAsync();
                await finder.BuildLooseFileCacheAsync();

                if(UseLoadOrder) 
                    finder.FindMissingAssets(UseLoadOrder);
                else
                    SelectedPlugins.Do(s =>
                    {
                        finder.FindMissingAssets(s);
                    });

                return finder.MissingAssets;
            }, token);

            missingAssets.Sort((first, second) => new FormKeyComparer().Compare(first.Record.FormKey, second.Record.FormKey));
            MissingAssets = missingAssets;

            IsWorking = false;

            /*missingAssets.Do(a =>
            {
                Utils.Log($"{a.Record.FormKey} is missing {a.Files.Aggregate((x,y) => $"{x},{y}")}");
            });*/
        }
    }
}
