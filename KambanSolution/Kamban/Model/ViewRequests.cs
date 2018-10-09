using Ui.Wpf.Common;

namespace Kamban.Model
{
    public class WizardViewRequest : ViewRequest
    {
        public bool InExistedFile { get; set; }
        public string Uri { get; set; }
    }

    public class BoardViewRequest : ViewRequest
    {
        public IProjectService PrjService { get; set; }
        public string NeededBoardName { get; set; }
    }
}
