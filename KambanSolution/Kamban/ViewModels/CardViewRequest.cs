using Kamban.ViewModels.Core;
using Ui.Wpf.Common;

namespace Kamban.ViewModels
{
    public class CardViewRequest : ViewRequest
    {
        public BoxViewModel Box { get; set; }
        public CardViewModel Card { get; set; }

        public int ColumnId { get; set; }
        public int RowId { get; set; }
        public BoardEditViewModel BoardVm { get; set; }
        public BoardViewModel Board { get; set; }
    }
}
