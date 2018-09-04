using Ui.Wpf.Common;

namespace Kamban.Models
{
    public class WizardViewRequest : ViewRequest
    {
        public bool InExistedFile { get; set; }
        public string Uri { get; set; }
    }

    public class BoardViewRequest : ViewRequest
    {
        public IScopeModel Scope { get; set; }
        public string SelectedBoardName { get; set; }
    }

}
