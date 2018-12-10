using Kamban.MatrixControl;
using Kamban.Model;
using ReactiveUI.Fody.Helpers;
using System.Linq;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class BoardEditForExportViewModel : ViewModelBase, IInitializableViewModel
    {
        public DbViewModel Db { get; set; }

        [Reactive] public bool EnableMatrix { get; set; }

        public ColumnViewModel[] Columns { get; set; }
        public RowViewModel[] Rows { get; set; }

        public ICard[] Cards { get; set; }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as BoardViewRequest;
            Db = request.Db;

            Columns = Db.Columns.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OrderBy(x => x.Order)
                .ToArray();

            Rows = Db.Rows.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OrderBy(x => x.Order)
                .ToArray();

            Cards = Db.Cards.Items
                .Where(x => x.BoardId == request.Board.Id)
                .OfType<ICard>()
                .ToArray();

            EnableMatrix = true;
        }
    }//end of class
}
