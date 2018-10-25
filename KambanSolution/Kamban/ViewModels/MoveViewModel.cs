using DynamicData;
using Kamban.MatrixControl;
using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Legacy;
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
        private IProjectService prjService;
        private CardViewModel card;
        private BoardViewModel boardVM;

        [Reactive] public Brush Background { get; set; }
        [Reactive] public bool IsOpened { get; set; }

        [Reactive] public SourceList<BoardInfo> AvailableBoards { get; set; }
        [Reactive] public BoardInfo SelectedBoard { get; set; }

        [Reactive] public SourceList<ColumnInfo> AvailableColumns { get; set; }
        [Reactive] public ColumnInfo SelectedColumn { get; set; }
        [Reactive] public SourceList<RowInfo> AvailableRows { get; set; }
        [Reactive] public RowInfo SelectedRow { get; set; }

        [Reactive] public string Url { get; set; }
        [Reactive] public string CardHeader { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand CopyToCommand { get; set; }
        public ReactiveCommand MoveToCommand { get; set; }

        public MoveViewModel()
        {
            AvailableBoards = new SourceList<BoardInfo>();
            AvailableColumns = new SourceList<ColumnInfo>();
            AvailableRows = new SourceList<RowInfo>();

            CancelCommand = ReactiveCommand.Create(() => IsOpened = false);
            CopyToCommand = ReactiveCommand.Create(CopyToCommandExecute);

            var canExecuteMove = this.WhenAnyValue(x => x.SelectedBoard,
                board => board != null && board.Id != card.BoardId);

            MoveToCommand = ReactiveCommand.Create(MoveToCommandExecute, canExecuteMove);

            this.WhenAnyValue(x => x.SelectedBoard)
                .Where(x => x != null)
                .Subscribe(async _ => await ChangeBoard(SelectedBoard.Id));
        }

        private void CopyToCommandExecute()
        {
            var issue = new Issue
            {
                Id = 0,
                Head = "[Copy] " + card.Header,
                ColumnId = SelectedColumn.Id,
                RowId = SelectedRow.Id,
                Order = card.Order,
                Color = card.Color,
                Body = card.Body,
                Created = card.Created,
                Modified = DateTime.Now,
                BoardId = SelectedBoard.Id
            };

            prjService.CreateOrUpdateIssueAsync(issue);

            var cvm = new CardViewModel(issue);
            boardVM.Cards.Add(cvm);

            IsOpened = false;
        }

        private void MoveToCommandExecute()
        {
            var issue = new Issue
            {
                Id = card.Id,
                Head = card.Header,
                ColumnId = SelectedColumn.Id,
                RowId = SelectedRow.Id,
                Order = card.Order,
                Color = card.Color,
                Body = card.Body,
                Created = card.Created,
                Modified = DateTime.Now,
                BoardId = SelectedBoard.Id
            };

            prjService.CreateOrUpdateIssueAsync(issue);
            boardVM.Cards.Remove(card);

            IsOpened = false;
        }

        private async Task ChangeProject(IProjectService ps)
        {
            prjService = ps;
            Url = ps.Uri;

            var boards = await ps.GetAllBoardsInFileAsync();
            AvailableBoards.Clear();
            AvailableBoards.AddRange(boards);

            await ChangeBoard(AvailableBoards.Items.First().Id);
        }

        private async Task ChangeBoard(int boardId)
        {
            SelectedBoard = AvailableBoards.Items.Where(x => x.Id == boardId).First();

            var columns = await prjService.GetColumnsByBoardIdAsync(SelectedBoard.Id);
            AvailableColumns.Clear();
            AvailableColumns.AddRange(columns);
            SelectedColumn = AvailableColumns.Items.First();

            var rows = await prjService.GetRowsByBoardIdAsync(SelectedBoard.Id);
            AvailableRows.Clear();
            AvailableRows.AddRange(rows);
            SelectedRow = AvailableRows.Items.First();
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            card = request.Card;
            boardVM = request.BoardVM;

            var str = request.Card.Header;
            var maxLen = str.Length >= 22 ? 22 : str.Length;
            CardHeader = "Select destination board and cell to make move \"" +
                request.Card.Header.Substring(0, maxLen) +
                (str.Length > 22 ? "..." : "") + "\"";

            Observable.FromAsync(() => ChangeProject(request.PrjService))
                .ObserveOnDispatcher()
                .Subscribe(async _ => await ChangeBoard(request.Board.Id));

            Title = $"Move issue {card.Header} to";
            IsOpened = true;
        }
    }//emd of classs
}
