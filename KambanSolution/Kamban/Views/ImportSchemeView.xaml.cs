using Kamban.ViewModels;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.Views
{
    /// <summary>
    /// Interaction logic for ImportSchemeView.xaml
    /// </summary>
    public partial class ImportSchemeView : IView
    {
        public ImportSchemeView(ImportSchemeViewModel vm, bool loadAll = true)
        {
            InitializeComponent();
            vm.LoadAll = loadAll;
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
