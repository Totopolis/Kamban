using Kamban.ViewModels;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.Views
{
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class SettingsView : IView
    {
        public SettingsView(SettingsViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = ViewModel;
        }

        public IViewModel ViewModel { get; set; }

        public void Configure(UiShowOptions options)
        {
            ViewModel.Title = options.Title;            
        }
    }
}
