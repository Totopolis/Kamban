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
        public int ColumnId { get; set; }
        public int RowId { get; set; }
        public CardViewModel Card { get; set; }
        public IProjectService PrjService { get; set; }
        public BoardViewModel BoardVM { get; set; }
        public BoardInfo Board { get; set; }
    }
}
