using Kamban.MatrixControl;
using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class MoveViewModel : ViewModelBase, IInitializableViewModel
    {
        private IBoardService scope;
        private CardViewModel card;

        [Reactive] public Brush Background { get; set; }
        [Reactive] public bool IsOpened { get; set; }

        [Reactive] public ReactiveList<BoardInfo> AvailableBoards { get; set; }
        [Reactive] public BoardInfo SelectedBoard { get; set; }

        [Reactive] public ReactiveList<ColumnInfo> AvailableColumns { get; set; }
        [Reactive] public ColumnInfo SelectedColumn { get; set; }
        [Reactive] public ReactiveList<RowInfo> AvailableRows { get; set; }
        [Reactive] public RowInfo SelectedRow { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SaveCommand { get; set; }

        public MoveViewModel()
        {
            AvailableBoards = new ReactiveList<BoardInfo>();
            AvailableColumns = new ReactiveList<ColumnInfo>();
            AvailableRows = new ReactiveList<RowInfo>();

            CancelCommand = ReactiveCommand.Create(() => IsOpened = false);
            SaveCommand = ReactiveCommand.Create(SaveCommandExecute);

            this.WhenAnyValue(x => x.SelectedBoard)
                .Where(x => x != null)
                .Subscribe(async _ => await ChangeBoard(SelectedBoard));
        }

        private void SaveCommandExecute()
        {

        }

        private async Task ChangeScope(IBoardService bs)
        {
            scope = bs;
            var boards = await bs.GetAllBoardsInFileAsync();
            AvailableBoards.Clear();
            AvailableBoards.AddRange(boards);

            await ChangeBoard(AvailableBoards.First());
        }

        private async Task ChangeBoard(BoardInfo bi)
        {
            SelectedBoard = bi;

            var columns = await scope.GetColumnsByBoardIdAsync(SelectedBoard.Id);
            AvailableColumns.Clear();
            AvailableColumns.AddRange(columns);
            SelectedColumn = AvailableColumns.First();

            var rows = await scope.GetRowsByBoardIdAsync(SelectedBoard.Id);
            AvailableRows.Clear();
            AvailableRows.AddRange(rows);
            SelectedRow = AvailableRows.First();
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            card = request.Card;

            Observable.FromAsync(() => ChangeScope(request.Scope))
                .ObserveOnDispatcher()
                .Subscribe();

            Title = $"Move issue {card.Header} to";
            IsOpened = true;
        }
    }//emd of classs
}
