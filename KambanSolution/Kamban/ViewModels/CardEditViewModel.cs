using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using Kamban.Common;
using Kamban.ViewModels.Core;
using Kamban.ViewRequests;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

namespace Kamban.ViewModels
{

    public enum CardEditResult
    {
        None,
        Created,
        Modified
    }

    public class CardEditViewModel : ViewModelBase, IInitializableViewModel
    {
        private BoxViewModel box;
        private BoardViewModel board;
        private int requestedColumnId;
        private int requestedRowId;

        [Reactive] public CardViewModel Card { get; set; }
        [Reactive] public CardEditResult Result { get; set; }

        [Reactive] public ReadOnlyObservableCollection<ColumnViewModel> AvailableColumns { get; set; }
        [Reactive] public ReadOnlyObservableCollection<RowViewModel> AvailableRows { get; set; }

        [Reactive] public string Head { get; set; }
        [Reactive] public string Body { get; set; }

        [Reactive] public ColumnViewModel SelectedColumn { get; set; }
        [Reactive] public RowViewModel SelectedRow { get; set; }

        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; set; }
        public ReactiveCommand<Unit, Unit> EnterCommand { get; set; }

        [Reactive] public bool IsOpened { get; set; }
        [Reactive] public int BodySelectionStart { get; set; }

        [Reactive] public Brush Background { get; set; }

        [Reactive] public ColorItem[] ColorItems { get; set; } = DefaultColorItems.Colors;
        
        [Reactive] public ColorItem SelectedColor { get; set; }

        public CardEditViewModel()
        {
            Card = null;

            var cardFilled = this.WhenAnyValue(
                t => t.Head, t => t.SelectedRow, t => t.SelectedColumn, t => t.SelectedColor,
                (sh, sr, sc, cc) =>
                sr != null && sc != null && !string.IsNullOrEmpty(sh) && cc != null);

            SaveCommand = ReactiveCommand.Create(SaveCommandExecute, cardFilled);

            CancelCommand = ReactiveCommand.Create(() =>
            {
                Result = CardEditResult.None;
                IsOpened = false;
            });

            EnterCommand = ReactiveCommand.Create(EnterCommandExecute);

            this.WhenAnyValue(x => x.SelectedColor)
                        .Where(x => x != null)
                        .Subscribe(_ => Background = SelectedColor.Brush);
        }

        private void EnterCommandExecute()
        {
            if (BodySelectionStart == 0)
                return;

            int currentSelection = BodySelectionStart;
            int strStart = BodySelectionStart;
            for (int i = BodySelectionStart - 1; i > 0; i--)
                if (Body[i] == '\n')
                {
                    strStart = i + 1;
                    break;
                }

            var subStr = Body.Substring(strStart, BodySelectionStart - strStart);
            string digitStr = new string(subStr.TakeWhile(char.IsDigit).ToArray());

            if (!string.IsNullOrEmpty(digitStr))
            {
                int digit = int.Parse(digitStr) + 1;
                string newStr = Environment.NewLine + $"{digit}. ";
                Body = Body.Insert(BodySelectionStart, newStr);
                BodySelectionStart = currentSelection + newStr.Length;
            }
        }

        private void SaveCommandExecute()
        {
            if (Card == null)
                Card = new CardViewModel
                {
                    Id = 0,
                    Created = DateTime.Now,
                    BoardId = board.Id,
                    Order = 0
                };

            Card.Header = Head;
            Card.Color = SelectedColor.SystemName;
            Card.Body = Body;
            Card.ColumnDeterminant = SelectedColumn.Id;
            Card.RowDeterminant = SelectedRow.Id;
            Card.Modified = DateTime.Now;

            IsOpened = false;
        }

        public void UpdateViewModel()
        {
            box.Columns
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == board.Id)
                .Sort(SortExpressionComparer<ColumnViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<ColumnViewModel> temp)
                .Subscribe();

            AvailableColumns = temp;

            box.Rows
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == board.Id)
                .Sort(SortExpressionComparer<RowViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<RowViewModel> temp2)
                .Subscribe();

            AvailableRows = temp2;

            SelectedColumn = AvailableColumns.First();
            SelectedRow = AvailableRows.First();

            if (Card == null)
            {
                Head = null;
                Body = null;
                SelectedColor = ColorItems.First();

                if (requestedColumnId != 0)
                    SelectedColumn = AvailableColumns.First(c => c.Id == requestedColumnId);

                if (requestedRowId != 0)
                    SelectedRow = AvailableRows.First(c => c.Id == requestedRowId);

                Result = CardEditResult.Created;
            }
            else
            {
                Head = Card.Header;
                Body = Card.Body;

                SelectedColumn = AvailableColumns.First(c => c.Id == Card.ColumnDeterminant);
                SelectedRow = AvailableRows.First(r => r.Id == Card.RowDeterminant);

                SelectedColor = ColorItems.
                    FirstOrDefault(c => c.SystemName == Card.Color)
                    ?? ColorItems.First();

                Result = CardEditResult.Modified;
            }
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as CardViewRequest;
            if (request == null)
                return;

            box = request.Box;
            board = request.Board;
            Card = request.Card;

            requestedColumnId = request.ColumnId;
            requestedRowId = request.RowId;

            UpdateViewModel();

            Title = $"Edit {Head}";
            IsOpened = true;
        }
    }//end of class
}
