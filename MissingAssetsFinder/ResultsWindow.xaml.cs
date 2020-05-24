using System.Collections.Generic;
using System.Reactive.Disposables;
using MissingAssetsFinder.Lib;
using ReactiveUI;

namespace MissingAssetsFinder
{
    public partial class ResultsWindow : IViewFor<ResultsWindowVM>
    {
        public ResultsWindow(IEnumerable<MissingAsset> missingAssets)
        {
            InitializeComponent();
            ViewModel = new ResultsWindowVM(this, missingAssets);

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, x => x.MissingAssets, x => x.ResultTreeView.ItemsSource)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.ExportCommand, x => x.ExportButton)
                    .DisposeWith(disposable);
            });
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ResultsWindowVM)value;
        }

        public ResultsWindowVM ViewModel { get; set; }
    }
}
