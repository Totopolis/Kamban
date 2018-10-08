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
        public CardViewModel Card { get; set; }
        public IBoardService Scope { get; set; }
        public BoardInfo Board { get; set; }
    }
}
