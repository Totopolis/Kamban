using Kamban.ViewModels;
using wpf.ui;

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
