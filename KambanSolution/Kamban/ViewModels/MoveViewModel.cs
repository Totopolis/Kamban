using AutoMapper;
using DynamicData;
using Kamban.MatrixControl;
using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
        private readonly IMapper mapper;

        private DbViewModel db;
        private CardViewModel card;

        [Reactive] public Brush Background { get; set; }
        [Reactive] public bool IsOpened { get; set; }

        [Reactive] public List<BoardViewModel> AvailableBoards { get; set; }
        [Reactive] public BoardViewModel SelectedBoard { get; set; }

        [Reactive] public List<ColumnViewModel> AvailableColumns { get; set; }
        [Reactive] public ColumnViewModel SelectedColumn { get; set; }
        [Reactive] public List<RowViewModel> AvailableRows { get; set; }
        [Reactive] public RowViewModel SelectedRow { get; set; }

        [Reactive] public string CardHeader { get; set; }

        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CopyToCommand { get; set; }
        public ReactiveCommand<Unit, Unit> MoveToCommand { get; set; }

        public MoveViewModel(IMapper mp)
        {
            mapper = mp;

            AvailableBoards = null;
            AvailableColumns = null;
            AvailableRows = null;

            CancelCommand = ReactiveCommand.Create(() => { IsOpened = false; });

            var canExecuteCopy = this.WhenAnyValue(
                x => x.SelectedBoard, x => x.SelectedRow, x => x.SelectedColumn,
                (brd, row, column) => brd != null && row != null && column != null);

            CopyToCommand = ReactiveCommand.Create(CopyToCommandExecute, canExecuteCopy);

            var canExecuteMove = this.WhenAnyValue(
                x => x.SelectedBoard, x => x.SelectedRow, x => x.SelectedColumn,
                (brd, row, column) => brd != null && brd.Id != card.BoardId 
                    && row != null && column != null);

            MoveToCommand = ReactiveCommand.Create(MoveToCommandExecute, canExecuteMove);

            this.WhenAnyValue(x => x.SelectedBoard)
                .Where(x => x != null)
                .Subscribe(_ =>
                {
                    AvailableColumns = db.Columns.Items
                        .Where(x => x.BoardId == SelectedBoard.Id)
                        .ToList();

                    AvailableRows = db.Rows.Items
                        .Where(x => x.BoardId == SelectedBoard.Id)
                        .ToList();

                    SelectedColumn = null;
                    SelectedRow = null;
                });
        }

        private void CopyToCommandExecute()
        {
            var copyCard = new CardViewModel
            {
                Id = 0,
                Header = "[Copy] " + card.Header,
                ColumnDeterminant = SelectedColumn.Id,
                RowDeterminant = SelectedRow.Id,
                Order = 0,
                Color = card.Color,
                Body = card.Body,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                BoardId = SelectedBoard.Id
            };

            if (card.BoardId == SelectedBoard.Id)
                db.Cards.Add(copyCard);

            IsOpened = false;
        }

        private void MoveToCommandExecute()
        {
            if (card.BoardId == SelectedBoard.Id)
                throw new NotImplementedException();

            card.BoardId = SelectedBoard.Id;
            card.ColumnDeterminant = SelectedColumn.Id;
            card.RowDeterminant = SelectedRow.Id;
            card.Order = 0;
            card.Modified = DateTime.Now;

            IsOpened = false;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            db = request.Db;
            card = request.Card;

            AvailableBoards = db.Boards.Items.ToList();
            SelectedBoard = AvailableBoards.First();

            var str = request.Card.Header;
            var maxLen = str.Length >= 22 ? 22 : str.Length;
            CardHeader = "Select destination board and cell to make move \"" +
                request.Card.Header.Substring(0, maxLen) +
                (str.Length > 22 ? "..." : "") + "\"";

            Title = $"Move issue {card.Header} to";
            IsOpened = true;
        }
    }//emd of classs
}
