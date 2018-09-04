using Kamban.ViewModels;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.Views
{
    public partial class WizardView : IView
    {
        public WizardView(WizardViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        public IViewModel ViewModel { get; set; }

        public void Configure(UiShowOptions options)
        {
            ViewModel.Title = options.Title;
        }

        private void Wizard_Cancel(object sender, System.Windows.RoutedEventArgs e)
        {
            (ViewModel as WizardViewModel)?.Close();
        }

        private void Wizard_Finish(object sender, Xceed.Wpf.Toolkit.Core.CancelRoutedEventArgs e)
        {
            (ViewModel as WizardViewModel)?.Create();
        }
    }
}
