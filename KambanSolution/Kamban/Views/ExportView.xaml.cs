using Kamban.ViewModels;
using wpf.ui;

namespace Kamban.Views
{
    public partial class ExportView : IView
    {
        public ExportView(ExportViewModel vm)
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
