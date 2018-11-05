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
    public class IssueViewRequest : ViewRequest
    {
        public DbViewModel Db { get; set; }
        public CardViewModel Card { get; set; }

        public int ColumnId { get; set; }
        public int RowId { get; set; }
        public BoardEditViewModel BoardVM { get; set; }
        public BoardViewModel Board { get; set; }
    }
}
