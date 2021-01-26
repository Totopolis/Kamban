using Kamban.Contracts;
using wpf.ui;

namespace Kamban.ViewRequests
{
    public class ImportSchemeViewRequest : ViewRequest
    {
        public ILoadRepository Repository { get; set; }

        public ImportSchemeViewRequest()
        {
        }
    }
}
