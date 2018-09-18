using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Kamban.Models;
using Kamban.SqliteLocalStorage.Entities;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using Ui.Wpf.KanbanControl.Dimensions;
using Ui.Wpf.KanbanControl.Elements.CardElement;

namespace Kamban.ViewModels
{
    public class BoardViewModel : ViewModelBase, IInitializableViewModel
    {
        private IScopeModel scope;

        private readonly IDialogCoordinator dialogCoordinator = DialogCoordinator.Instance;

        [Reactive] private LocalIssue SelectedIssue { get; set; }
        [Reactive] private RowInfo SelectedRow { get; set; }
        [Reactive] private ColumnInfo SelectedColumn { get; set; }

        [Reactive] public IDimension VerticalDimension { get; internal set; }
        [Reactive] public IDimension HorizontalDimension { get; internal set; }
        [Reactive] public ReactiveList<BoardInfo> BoardsInFile { get; set; }
        [Reactive] public BoardInfo CurrentBoard { get; set; }
        [Reactive] public ICardContent CardContent { get; private set; }

        [Reactive] public IssueViewModel IssueViewModel { get; set; }

        public ReactiveList<string> Entities { get; }
            = new ReactiveList<string>() {"Задачу", "Столбец", "Строку"};

        public ReactiveList<LocalIssue> Issues { get; internal set; }

        public ReactiveCommand RefreshCommand { get; set; }

        public ReactiveCommand DeleteCommand { get; set; }

        public ReactiveCommand<object, Unit> UpdateCardCommand { get; set; }

        public ReactiveCommand<object, Unit> UpdateHorizontalHeaderCommand { get; set; }

        public ReactiveCommand<object, Unit> UpdateVerticalHeaderCommand { get; set; }

        public ReactiveCommand<string, Unit> AddNewElementCommand { get; set; }

        public ReactiveCommand IssueSelectCommand { get; set; }

        public ReactiveCommand RowHeaderSelectCommand { get; set; }

        public ReactiveCommand ColumnHeaderSelectCommand { get; set; }

        public BoardViewModel()
        {
            Issues = new ReactiveList<LocalIssue>();
            BoardsInFile = new ReactiveList<BoardInfo>();

            RefreshCommand =
                ReactiveCommand.Create(RefreshContent);

            var isSelectedEditable = this.WhenAnyValue(t => t.SelectedIssue, t => t.SelectedColumn,
                t => t.SelectedRow,
                (si, sc, sr) =>
                    si != null || sc != null ||
                    sr != null); //TODO :add selectcommand when click uneditable with nulling all "selected" fields

            DeleteCommand = ReactiveCommand.CreateFromTask(DeleteElement, isSelectedEditable);

            UpdateCardCommand = ReactiveCommand.Create<object>(UpdateCard);

            UpdateHorizontalHeaderCommand = ReactiveCommand
                .Create<object>(async ob => await UpdateHorizontalHeader(ob));

            UpdateVerticalHeaderCommand = ReactiveCommand
                .Create<object>(async ob => await UpdateVerticalHeader(ob));

            AddNewElementCommand =
                ReactiveCommand.CreateFromTask<string>(async name => await AddNewElement(name));

            IssueSelectCommand = ReactiveCommand.Create<object>(o =>
            {
                SelectedIssue = o as LocalIssue;

                if (SelectedIssue == null) return;

                SelectedColumn = null;
                SelectedRow = null;
            });

            RowHeaderSelectCommand = ReactiveCommand.Create<object>(o =>
            {
                SelectedRow = scope.GetSelectedRow(o.ToString());

                if (SelectedRow == null) return;

                SelectedColumn = null;
                SelectedIssue = null;
            });

            ColumnHeaderSelectCommand = ReactiveCommand.Create<object>(o =>
            {
                SelectedColumn = scope.GetSelectedColumn(o.ToString());

                if (SelectedColumn == null) return;

                SelectedRow = null;
                SelectedIssue = null;
            });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .Subscribe(async _ => await RefreshContent());

            this.ObservableForProperty(w => w.IssueViewModel.IssueChanged)
                .Where(ch => ch.Value == true)
                .Subscribe(async _ => await RefreshContent());
        }

        private async Task RefreshContent()
        {
            Issues.Clear();

            VerticalDimension = null;
            VerticalDimension = await scope.GetRowHeadersAsync(CurrentBoard.Id);

            HorizontalDimension = null;
            HorizontalDimension = await scope.GetColumnHeadersAsync(CurrentBoard.Id);

            CardContent = scope.GetCardContent();

            Issues.PublishCollection(await scope.GetIssuesByBoardIdAsync(CurrentBoard.Id));
        }

        private async Task DeleteElement()
        {
            var element = SelectedIssue != null ? "задачу" :
                SelectedColumn          != null ? "весь столбец" : "всю строку";

            var ts = await dialogCoordinator.ShowMessageAsync(this, "Warning",
                $"Вы действительно хотите удалить {element}?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            if (SelectedIssue != null)
                scope.DeleteIssueAsync(SelectedIssue.Id);
            else if (SelectedRow != null)
                scope.DeleteRowAsync(SelectedRow.Id);
            else if (SelectedColumn != null)
                scope.DeleteColumnAsync(SelectedColumn.Id);

            await RefreshContent();
        }

        private void UpdateCard(object o)
        {
            if (o is LocalIssue)
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = SelectedIssue.Id,
                    Scope = scope,
                    Board = CurrentBoard
                });
            else if (o is null)
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = 0,
                    Scope = scope,
                    Board = CurrentBoard
                });
        }

        private async Task UpdateHorizontalHeader(object o)
        {
            var newName = await ShowColumnNameInput();

            var column = scope.GetSelectedColumn(o.ToString());

            if (!string.IsNullOrEmpty(newName))
            {
                column.Name = newName;
                scope.CreateOrUpdateColumnAsync(column);
            }

            await RefreshContent();
        }

        private async Task UpdateVerticalHeader(object o)
        {
            var newName = await ShowRowNameInput();

            var row = scope.GetSelectedRow(o.ToString());

            if (!string.IsNullOrEmpty(newName))
            {
                row.Name = newName;
                scope.CreateOrUpdateRowAsync(row);
            }

            await RefreshContent();
        }

        private async Task AddNewElement(string elementName)
        {
            if (elementName == "Задачу")
            {
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = 0,
                    Scope = scope,
                    Board = CurrentBoard
                });
            }
            else if (elementName == "Строку")
            {
                var newName = await ShowRowNameInput();

                if (!string.IsNullOrEmpty(newName))
                {
                    var newRow = new RowInfo {Name = newName, Board = CurrentBoard};
                    scope.CreateOrUpdateRowAsync(newRow);
                }
            }
            else if (elementName == "Столбец")
            {
                var newName = await ShowColumnNameInput();

                if (!string.IsNullOrEmpty(newName))
                {
                    var newColumn = new ColumnInfo {Name = newName, Board = CurrentBoard};
                    scope.CreateOrUpdateColumnAsync(newColumn);
                }
            }

            await RefreshContent();
        }

        private async Task<string> ShowColumnNameInput()
        {
            return await dialogCoordinator
                .ShowInputAsync(this, "ColumnRed", "Введите название столбца",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "подтвердить",
                        NegativeButtonText = "отмена",
                        DefaultText = SelectedColumn?.Name
                    });
        }

        private async Task<string> ShowRowNameInput()
        {
            return await dialogCoordinator
                .ShowInputAsync(this, "RowRed", "Введите название строки",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "подтвердить",
                        NegativeButtonText = "отмена",
                        DefaultText = SelectedRow?.Name,
                        DialogResultOnCancel = MessageDialogResult.Negative

                    });
        } //TODO: add some logic preventing empty name

        public void Initialize(ViewRequest viewRequest)
        {
            var request = viewRequest as BoardViewRequest;
            IssueViewModel = new IssueViewModel();

            scope = request.Scope;

            Observable.FromAsync(() => scope.GetAllBoardsInFileAsync())
                .ObserveOnDispatcher()
                .Subscribe(boards =>
                {
                    BoardsInFile.PublishCollection(boards);

                    CurrentBoard = !string.IsNullOrEmpty(request.SelectedBoardName)
                        ? BoardsInFile.First(board => board.Name == request.SelectedBoardName)
                        : BoardsInFile.First();

                    Issues.Clear();

                    Observable.FromAsync(() => scope.GetRowHeadersAsync(CurrentBoard.Id))
                        .ObserveOnDispatcher()
                        .Subscribe(vert => VerticalDimension = vert);

                    Observable.FromAsync(() => scope.GetColumnHeadersAsync(CurrentBoard.Id))
                        .ObserveOnDispatcher()
                        .Subscribe(horiz => HorizontalDimension = horiz);

                    CardContent = scope.GetCardContent();

                    Observable.FromAsync(() => scope.GetIssuesByBoardIdAsync(CurrentBoard.Id))
                        .ObserveOnDispatcher()
                        .Subscribe(issues =>
                            Issues.AddRange(issues));
                });
        }
    }//end of class
}
