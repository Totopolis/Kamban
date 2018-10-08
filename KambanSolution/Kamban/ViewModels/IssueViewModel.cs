using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using AutoMapper;
using Kamban.MatrixControl;
using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using Brush = System.Windows.Media.Brush;
using ColorConverter = System.Windows.Media.ColorConverter;
using WpfColor = System.Windows.Media.Color;

namespace Kamban.ViewModels
{
    public class ColorItem
    {
        public SolidColorBrush Brush { get; private set; }
        public string Name { get; private set; }
        public string SystemName => Brush.Color.ToString();

        public static ColorItem I(string colorName)
        {
            ColorConverter converter = new ColorConverter();
            Color color = (Color)converter.ConvertFromInvariantString(colorName);

            return new ColorItem
            {
                Brush = new SolidColorBrush(color),
                Name = colorName
            };
        }
    }
    
    public enum IssueEditResult
    {
        None,
        Created,
        Deleted,
        Modified
    }

    public class IssueViewModel : ViewModelBase, IInitializableViewModel
    {
        private IBoardService scope;
        private BoardInfo board;

        [Reactive] public CardViewModel Card { get; set; }
        [Reactive] public IssueEditResult Result { get; set; }

        public ReactiveList<ColumnInfo> AvailableColumns { get; set; }
        public ReactiveList<RowInfo> AvailableRows { get; set; }

        [Reactive] public string Head { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public RowInfo SelectedRow { get; set; }
        [Reactive] public ColumnInfo SelectedColumn { get; set; }

        public ReactiveCommand CancelCommand { get; set; }
        public ReactiveCommand SaveCommand { get; set; }
        public ReactiveCommand DeleteCommand { get; set; }

        [Reactive] public bool IsOpened { get; set; }

        [Reactive] public Brush Background { get; set; }

        [Reactive] public ColorItem[] ColorItems { get; set; } =
        {
            ColorItem.I("LemonChiffon"),
            ColorItem.I("WhiteSmoke"),
            ColorItem.I("NavajoWhite"),
            ColorItem.I("HoneyDew")
        };

        [Reactive] public ColorItem SelectedColor { get; set; }

        public IssueViewModel()
        {
            Card = null;
            Result = IssueEditResult.None;

            AvailableColumns = new ReactiveList<ColumnInfo>();
            AvailableRows = new ReactiveList<RowInfo>();

            var issueFilled = this.WhenAnyValue(
                t => t.Head, t => t.SelectedRow, t => t.SelectedColumn, t => t.SelectedColor,
                (sh, sr, sc, cc) =>
                sr != null && sc != null && !string.IsNullOrEmpty(sh) && cc != null);

            SaveCommand = ReactiveCommand.Create(() =>
            {
                var editedIssue = new Issue
                {
                    Id = Card == null ? 0 : Card.Id,
                    Head = Head,
                    ColumnId = SelectedColumn.Id,
                    RowId = SelectedRow.Id,
                    Color = SelectedColor.SystemName,
                    Body = Body,
                    Created = Card == null ? DateTime.Now : Card.Created,
                    Modified = DateTime.Now,
                    BoardId = board.Id
                };

                scope.CreateOrUpdateIssueAsync(editedIssue);

                if (Card == null)
                {
                    Card = new CardViewModel(editedIssue);
                    Result = IssueEditResult.Created;
                }
                else
                {
                    Card.Header = Head;
                    Card.Body = Body;
                    Card.ColumnDeterminant = SelectedColumn.Id;
                    Card.RowDeterminant = SelectedRow.Id;
                    Card.Color = SelectedColor.SystemName;
                    Card.Modified = DateTime.Now;

                    Result = IssueEditResult.Modified;
                }

                IsOpened = false;
            }, issueFilled);

            CancelCommand = ReactiveCommand.Create(() => IsOpened = false);

            DeleteCommand = ReactiveCommand.Create(Delete);

            this.WhenAnyValue(x => x.SelectedColor)
                        .Where(x => x != null)
                        .Subscribe(_ => Background = SelectedColor.Brush);
        }

        public void Delete()
        {
            if (Card == null)
                return;

            scope.DeleteIssueAsync(Card.Id);

            Result = IssueEditResult.Deleted;
            IsOpened = false;
        }

        public async Task UpdateViewModel()
        {
            var columns = await scope.GetColumnsByBoardIdAsync(board.Id);
            var rows = await scope.GetRowsByBoardIdAsync(board.Id);

            AvailableColumns.PublishCollection(columns);
            SelectedColumn = AvailableColumns.First();
            AvailableRows.PublishCollection(rows);
            SelectedRow = AvailableRows.First();

            if (Card == null)
            {
                Head = null;
                Body = null;
                SelectedColor = ColorItems.First();
            }
            else
            {
                Head = Card.Header;
                Body = Card.Body;

                SelectedColumn = AvailableColumns
                    .First(c => c.Id == (int)Card.ColumnDeterminant);
                SelectedRow = AvailableRows
                    .First(r => r.Id == (int)Card.RowDeterminant);

                SelectedColor = ColorItems.
                    FirstOrDefault(c => c.SystemName == Card.Color)
                    ?? ColorItems.First();
            }
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            scope = request.Scope;
            board = request.Board;
            Card = request.Card;
            Result = IssueEditResult.None;

            Observable.FromAsync(() => UpdateViewModel())
                .ObserveOnDispatcher()
                .Subscribe();

            Title = $"Issue edit {Head}";
            IsOpened = true;
        }
    }//end of class
}
