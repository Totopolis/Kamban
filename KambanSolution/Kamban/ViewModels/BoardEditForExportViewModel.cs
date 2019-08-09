using System.Linq;
using Kamban.ViewModels.Core;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class BoardEditForExportViewModel : ViewModelBase, IInitializableViewModel
    {
        public BoxViewModel Box { get; set; }

        [Reactive] public bool ShowCardIds { get; set; }
        [Reactive] public bool EnableMatrix { get; set; }

        public ColumnViewModel[] Columns { get; set; }
        public RowViewModel[] Rows { get; set; }

        public ICard[] Cards { get; set; }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as BoardViewRequest;
            Box = request.Box;

            Columns = Box.Columns.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OrderBy(x => x.Order)
                .ToArray();

            Rows = Box.Rows.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OrderBy(x => x.Order)
                .ToArray();

            Cards = Box.Cards.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OfType<ICard>()
                .ToArray();

            ShowCardIds = request.ShowCardIds;
            EnableMatrix = true;
        }
    }//end of class
}
