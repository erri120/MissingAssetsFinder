using System.ComponentModel;
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
                this.Bind(ViewModel, x => x.UseLoadOrder, x => x.UseLoadOrderCheckBox.IsChecked)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.Log, x => x.LogListBox.ItemsSource)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.SelectedDataPath, x => x.DataFolderTextBox.Text)
                    .DisposeWith(disposable);
                this.OneWayBind(ViewModel, x => x.SelectedPlugins, x => x.PluginsListBox.ItemsSource)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.SelectDataFolder, x => x.SelectDataFolderButton)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.SelectPlugins, x => x.SelectPluginButton)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.Start, x => x.StartButton)
                    .DisposeWith(disposable);
                this.BindCommand(ViewModel, x => x.ViewResults, x => x.ViewButton)
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
