using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kamban.MatrixControl;
using Kamban.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ui.Wpf.Common;


namespace Kamban.ViewModels
{
    class HeaderPropertyViewRequest : ViewRequest
    {
        public IDim Header { get; set; }
        public DbViewModel Db { get; set; }
        public BoardEditViewModel BoardVM { get; set; }
        public BoardViewModel Board { get; set; }

    }
}
