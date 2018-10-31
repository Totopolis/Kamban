using Ui.Wpf.Common;

namespace Kamban.Model
{
    public class WizardViewRequest : ViewRequest
    {
        public string Uri { get; set; }
    }

    public class BoardViewRequest : ViewRequest
    {
        public DbViewModel Db { get; set; }
    }
}
