using Ui.Wpf.Common;

namespace Kamban.ViewModels.Core
{
    public class WizardViewRequest : ViewRequest
    {
        public string Uri { get; set; }
    }

    public class BoardViewRequest : ViewRequest
    {
        public BoxViewModel Box { get; set; }
        public BoardViewModel Board { get; set; }
    }
}
