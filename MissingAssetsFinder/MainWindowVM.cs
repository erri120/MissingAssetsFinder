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

        public ReactiveCommand<Unit, Unit> SelectDataFolder;
        public ReactiveCommand<Unit, Unit> SelectPlugins;
        public ReactiveCommand<Unit, Unit> Start;

        public ObservableCollectionExtended<string> Log { get; } = new ObservableCollectionExtended<string>();

        public MainWindowVM(MainWindow mainWindow)
        {
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
                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
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
                dialog.Filters.Add(new CommonFileDialogFilter("Plugin", ".esp"));

                if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
                Utils.Log($"Selected {dialog.FileNames.Count()} files");
                SelectedPlugins = dialog.FileNames.ToList();
            }, this.WhenAny(x => x.IsWorking).Select(x => !x));

            Start = ReactiveCommand.CreateFromTask(FindMissingAssets,
                this.WhenAny(x => x.IsWorking).CombineLatest(
                    this.WhenAny(x => x.SelectedDataPath).Select(x => x.IsEmpty()),
                    this.WhenAny(x => x.SelectedPlugins).Select(x => x.All(y => y.IsEmpty())),
                    (isWorking, dataPathEmpty, pluginPathEmpty) => !isWorking && !dataPathEmpty && !pluginPathEmpty));
        }

        public async Task FindMissingAssets(CancellationToken token)
        {
            IsWorking = true;

            var missingAssets = await Task.Run(async () =>
            {
                var finder = new Finder(SelectedDataPath);

                await finder.BuildBSACacheAsync();
                await finder.BuildLooseFileCacheAsync();

                SelectedPlugins.Do(s =>
                {
                    finder.FindMissingAssets(s);
                });

                return finder.MissingAssets;
            }, token);

            IsWorking = false;

            missingAssets.Do(a =>
            {
                Utils.Log($"{a.Record.FormKey} is missing {a.Files.Aggregate((x,y) => $"{x},{y}")}");
            });
        }
    }
}
