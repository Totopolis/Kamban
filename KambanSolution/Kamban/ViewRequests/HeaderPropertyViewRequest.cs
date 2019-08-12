using Kamban.ViewModels;
using Kamban.ViewModels.Core;
using Ui.Wpf.Common;



namespace Kamban.ViewModels
{
    class HeaderPropertyViewRequest : ViewRequest
    {
        public IDim Header { get; set; }
        public BoxViewModel Box { get; set; }
        public BoardEditViewModel BoardVM { get; set; }
        public BoardViewModel Board { get; set; }

    }
}
