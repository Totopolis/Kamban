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
        Modified
    }

    public class IssueViewModel : ViewModelBase, IInitializableViewModel
    {
        private IProjectService prjService;
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

            AvailableColumns = new ReactiveList<ColumnInfo>();
            AvailableRows = new ReactiveList<RowInfo>();

            var issueFilled = this.WhenAnyValue(
                t => t.Head, t => t.SelectedRow, t => t.SelectedColumn, t => t.SelectedColor,
                (sh, sr, sc, cc) =>
                sr != null && sc != null && !string.IsNullOrEmpty(sh) && cc != null);

            SaveCommand = ReactiveCommand.Create(() => SaveCommandExecute(), issueFilled);

            CancelCommand = ReactiveCommand.Create(() =>
            {
                Result = IssueEditResult.None;
                IsOpened = false;
            });

            this.WhenAnyValue(x => x.SelectedColor)
                        .Where(x => x != null)
                        .Subscribe(_ => Background = SelectedColor.Brush);
        }

        private void SaveCommandExecute()
        {
            var cvm = Card;

            if (cvm == null)
            {
                var issue = new Issue
                {
                    Id = 0,
                    Created = DateTime.Now,
                    BoardId = board.Id
                };

                cvm = new CardViewModel(issue);
            }

            cvm.Header = Head;
            cvm.Color = SelectedColor.SystemName;
            cvm.Body = Body;
            cvm.ColumnDeterminant = SelectedColumn.Id;
            cvm.RowDeterminant = SelectedRow.Id;
            cvm.Modified = DateTime.Now;

            if (Card == null)
                Card = cvm;

            IsOpened = false;
        }

        public async Task UpdateViewModel()
        {
            var columns = await prjService.GetColumnsByBoardIdAsync(board.Id);
            var rows = await prjService.GetRowsByBoardIdAsync(board.Id);

            AvailableColumns.PublishCollection(columns);
            SelectedColumn = AvailableColumns.First();
            AvailableRows.PublishCollection(rows);
            SelectedRow = AvailableRows.First();

            if (Card == null)
            {
                Head = null;
                Body = null;
                SelectedColor = ColorItems.First();

                Result = IssueEditResult.Created;
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

                Result = IssueEditResult.Modified;
            }
        }

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as IssueViewRequest;
            if (request == null)
                return;

            prjService = request.PrjService;
            board = request.Board;
            Card = request.Card;

            Observable.FromAsync(() => UpdateViewModel())
                .ObserveOnDispatcher()
                .Subscribe();

            Title = $"Issue edit {Head}";
            IsOpened = true;
        }
    }//end of class
}
