using Kamban.ViewModels.Core;
using wpf.ui;

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
