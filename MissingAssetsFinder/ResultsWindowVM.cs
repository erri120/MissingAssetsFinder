using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using MissingAssetsFinder.Lib;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MissingAssetsFinder
{
    public class ResultsWindowVM : ViewModel
    {
        public ObservableCollectionExtended<MissingAsset> MissingAssets;

        private readonly Window _window;

        public ReactiveCommand<Unit, Unit> ExportCommand;

        [Reactive]
        public bool IsWorking { get; set; }

        public ResultsWindowVM(Window window, IEnumerable<MissingAsset> missingAssets)
        {
            _window = window;
            MissingAssets = new ObservableCollectionExtended<MissingAsset>(missingAssets);

            ExportCommand = ReactiveCommand.CreateFromTask(Export, this.WhenAny(x => x.IsWorking).Select(x => !x));
        }

        public async Task Export(CancellationToken token)
        {
            IsWorking = true;
            try
            {
                var dialog = new CommonOpenFileDialog
                {
                    Title = "Select output",
                    IsFolderPicker = false,
                    Multiselect = false,
                    AddToMostRecentlyUsedList = false,
                    EnsureFileExists = false,
                    EnsurePathExists = true,
                    EnsureValidNames = true
                };
                dialog.Filters.Add(new CommonFileDialogFilter("Output", ".csv,.html"));

                if (dialog.ShowDialog(_window) != CommonFileDialogResult.Ok) return;
                var file = dialog.FileName;

                if (File.Exists(file))
                {
                    Utils.Log($"File {file} already exists, deleting...");
                    File.Delete(file);
                }

                var extension = Path.GetExtension(file);

                await using var stream = File.OpenWrite(file);
                await using var writer = new StreamWriter(stream);

                if (extension == ".csv")
                {
                    MissingAssets.Do(a =>
                    {
                        var s = $"{a.Record.FormKey.IDString()},";
                        a.Files.Do(f =>
                        {
                            s += $"{f},";
                        });
                        s = s[0..^1];

                        writer.WriteLine(s);
                    });
                } else if (extension == ".html")
                {
                    await writer.WriteAsync("<!DOCTYPE html><html><head><link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.5.0/css/bootstrap.min.css\" integrity=\"sha384-9aIt2nRpC12Uk9gS9baDl411NQApFmC26EwAOH8WgZl5MYYxFfc+NcPb1dKGj7Sk\" crossorigin=\"anonymous\"></head><body>");
                    await writer.WriteLineAsync("<div class=\"container\">");
                    await writer.WriteLineAsync($"<h1>Missing Assets Finder Report ({DateTime.Now:G})</h1></br>");

                    await writer.WriteLineAsync($"<ul><li>Records with missing files: {MissingAssets.Count}</li>");
                    await writer.WriteLineAsync($"<li>Total missing files: {MissingAssets.Select(x => x.Files.Count).Aggregate((x, y) => x+y)}</li></ul></br>");

                    await writer.WriteLineAsync("<h2>Records:</h2></br>");
                    await writer.WriteAsync("<input type=\"text\" class=\"form-control\" id=\"searchInput\" onkeyup=\"searchFunc()\" placeholder=\"Search for IDs...\"/>\n<script>\nfunction searchFunc(){const a=document.getElementById(\"searchInput\"),b=a.value.toUpperCase(),c=document.getElementById(\"table\"),d=c.getElementsByTagName(\"tr\");for(let a,c=0;c<d.length;c++){if(a=d[c].getElementsByTagName(\"td\")[0],!a)continue;let e=a.textContent||a.innerText;d[c].style.display=-1<e.toUpperCase().indexOf(b)?\"\":\"none\"}}\n</script>");
                    await writer.WriteLineAsync("<table id=\"table\" class=\"table\"><tbody><tr><th scope=\"col\">ID</th><th scope=\"col\">Files</th></tr>");

                    MissingAssets.Do(a =>
                    {
                        writer.WriteLine($"<tr><td>{a.Record.FormKey}</td><td><ul>");

                        a.Files.Do(f =>
                        {
                            writer.WriteLine($"<li>{f}</li>");
                        });

                        writer.WriteLine($"</ul></td></tr>");
                    });

                    await writer.WriteAsync("</div></body></html>");
                }

                Utils.Log($"Exported results to {file}");
            }
            catch (Exception e)
            {
                Utils.Log($"Exception: {e}");
            }
            finally
            {
                IsWorking = false;
            }
        }
    }
}
