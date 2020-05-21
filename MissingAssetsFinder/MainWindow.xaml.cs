using System.Reactive.Disposables;
using ReactiveUI;

namespace MissingAssetsFinder
{
    public partial class MainWindow : IViewFor<MainWindowVM>
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowVM(this);

            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel, x => x.Log, x => x.LogListBox.ItemsSource)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.SelectedDataPath, x => x.DataFolderTextBox.Text)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.SelectedPluginPath, x => x.PluginTextBox.Text)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.SelectDataFolder, x => x.SelectDataFolderButton)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.SelectPlugin, x => x.SelectPluginButton)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.Start, x => x.StartButton)
                    .DisposeWith(disposable);
            });
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainWindowVM)value;
        }

        public MainWindowVM ViewModel { get; set; }
    }
}
