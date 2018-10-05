using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Kamban.MatrixControl;
using Kamban.Model;
using Kamban.Views;
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
        private IBoardService scope;

        private readonly IDialogCoordinator dialogCoordinator = DialogCoordinator.Instance;

        // Actual

        public ReactiveList<IDim> Columns { get; set; }
        public ReactiveList<IDim> Rows { get; set; }
        public ReactiveList<ICard> Cards { get; set; }

        public ReactiveCommand<CardViewModel, Unit> CardClickCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NormalizeGridCommand { get; set; }

        // Obsolete

        [Reactive] private RowInfo SelectedRow { get; set; }
        [Reactive] private ColumnInfo SelectedColumn { get; set; }

        [Reactive] public ReactiveList<BoardInfo> BoardsInFile { get; set; }
        [Reactive] public BoardInfo CurrentBoard { get; set; }

        [Reactive] public IssueViewModel IssueViewModel { get; set; }

        public ReactiveCommand<object, Unit> UpdateCardCommand { get; set; }
        public ReactiveCommand<object, Unit> UpdateHorizontalHeaderCommand { get; set; }
        public ReactiveCommand<object, Unit> UpdateVerticalHeaderCommand { get; set; }

        public ReactiveCommand<Unit, Unit> CreateTiketCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CreateColumnCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CreateRowCommand { get; set; }

        public ReactiveCommand<Unit, Unit> AddBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NextBoardCommand { get; set; }
        public ReactiveCommand<object, Unit> SelectBoardCommand { get; set; }

        private readonly IShell shell;

        public BoardViewModel(IShell shell)
        {
            this.shell = shell;

            Columns = new ReactiveList<IDim>();
            Rows = new ReactiveList<IDim>();
            Cards = new ReactiveList<ICard>();

            BoardsInFile = new ReactiveList<BoardInfo>();
            IssueViewModel = new IssueViewModel();

            /*var isSelectedEditable = this.WhenAnyValue(t => t.SelectedIssue, t => t.SelectedColumn,
                t => t.SelectedRow,
                (si, sc, sr) =>
                    si != null || sc != null ||
                    sr != null); 
            */
            //TODO :add selectcommand when click uneditable with nulling all "selected" fields

            //DeleteCommand = ReactiveCommand.CreateFromTask(DeleteElement, isSelectedEditable);

            CardClickCommand = ReactiveCommand.Create<CardViewModel>(CardClickCommandExecute);

            UpdateCardCommand = ReactiveCommand.Create<object>(UpdateCard);

            UpdateHorizontalHeaderCommand = ReactiveCommand
                .Create<object>(async ob => await UpdateHorizontalHeader(ob));

            UpdateVerticalHeaderCommand = ReactiveCommand
                .Create<object>(async ob => await UpdateVerticalHeader(ob));

            //AddNewElementCommand =
            //    ReactiveCommand.CreateFromTask<string>(async name => await AddNewElement(name));

            CreateTiketCommand = ReactiveCommand
                .Create(() =>
               {
                   IssueViewModel.Initialize(new IssueViewRequest
                   {
                       IssueId = 0,
                       Scope = scope,
                       Board = CurrentBoard
                   });
               });

            CreateColumnCommand = ReactiveCommand
                .CreateFromTask(async () =>
               {
                   var newName = await ShowColumnNameInput();

                   if (!string.IsNullOrEmpty(newName))
                   {
                       var newColumn = new ColumnInfo { Name = newName, Board = CurrentBoard };
                       scope.CreateOrUpdateColumnAsync(newColumn);
                   }

                   await RefreshContent();
               });

            CreateRowCommand = ReactiveCommand
                .CreateFromTask(async () =>
               {
                   var newName = await ShowRowNameInput();

                   if (!string.IsNullOrEmpty(newName))
                   {
                       var newRow = new RowInfo { Name = newName, Board = CurrentBoard };
                       scope.CreateOrUpdateRowAsync(newRow);
                   }

                   await RefreshContent();
               });

            AddBoardCommand = ReactiveCommand.Create(() =>
            {
                this.shell.ShowView<WizardView>(new WizardViewRequest
                {
                    ViewId = $"Creating new board in {scope.Uri}",
                    InExistedFile = true,
                    Uri = scope.Uri
                });
            });

            NextBoardCommand = ReactiveCommand.Create(() =>
            {
                int indx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = indx < BoardsInFile.Count - 1 ?
                    BoardsInFile[indx + 1] :
                    BoardsInFile[0];
            });

            SelectBoardCommand = ReactiveCommand
                .Create((object mi) =>
                {
                    string name = ((MenuItem)mi).Header as string;
                    CurrentBoard = BoardsInFile.Where(x => x.Name == name).First();
                });

            Columns
                .ItemChanged
                .Subscribe(ob => 
                {
                    var cvm = ob.Sender as ColumnViewModel;

                    var ci = new ColumnInfo
                    {
                        Id = cvm.Id,
                        Board = CurrentBoard,
                        Name = cvm.Caption,
                        Width = cvm.Size
                    };

                    scope.CreateOrUpdateColumnAsync(ci);
                });

            Rows
                .ItemChanged
                .Subscribe(ob =>
                {
                    var rvm = ob.Sender as RowViewModel;

                    var ri = new RowInfo
                    {
                        Id = rvm.Id,
                        Board = CurrentBoard,
                        Name = rvm.Caption,
                        Height = rvm.Size
                    };

                    scope.CreateOrUpdateRowAsync(ri);
                });

            NormalizeGridCommand = ReactiveCommand.Create(() => { });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .ObserveOnDispatcher()
                .Subscribe(async _ => await RefreshContent());

            this.ObservableForProperty(w => w.IssueViewModel.IssueChanged)
                .Where(ch => ch.Value == true)
                .ObserveOnDispatcher()
                .Subscribe(async _ => await RefreshContent());
        }

        private async Task RefreshContent()
        {
            try
            {
                var columns = await scope.GetColumnsByBoardIdAsync(CurrentBoard.Id);
                var rows = await scope.GetRowsByBoardIdAsync(CurrentBoard.Id);
                var issues = await scope.GetIssuesByBoardIdAsync(CurrentBoard.Id);

                Columns.ChangeTrackingEnabled = false;
                Rows.ChangeTrackingEnabled = false;

                Columns.Clear();
                Rows.Clear();
                Cards.Clear();

                Columns.AddRange(columns.Select(x => new ColumnViewModel(x)));
                Rows.AddRange(rows.Select(x => new RowViewModel(x)));

                Rows.ChangeTrackingEnabled = true;
                Columns.ChangeTrackingEnabled = true;

                Cards.AddRange(issues.Select(x => new CardViewModel(x)));
            }
            catch(Exception ex)
            {
                Title = "RefreshContent: " + ex.Message;
            }
        }

        public void CardClickCommandExecute(CardViewModel cvm)
        {
            try
            {
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = cvm.Id,
                    Scope = scope,
                    Board = CurrentBoard
                });
            }
            catch(Exception ex)
            {
                Title = ex.Message;
            }
        }

        /*private async Task DeleteElement()
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
        }*/

        private void UpdateCard(object o)
        {
            var iss = o as Issue;

            if (iss != null)
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = iss.Id,
                    Scope = scope,
                    Board = CurrentBoard
                });

            /*if (o is Issue)
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
                });*/
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

        private async Task<string> ShowColumnNameInput()
        {
            return await dialogCoordinator
                .ShowInputAsync(this, "ColumnRed", "Input column name",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = SelectedColumn?.Name
                    });
        }

        private async Task<string> ShowRowNameInput()
        {
            return await dialogCoordinator
                .ShowInputAsync(this, "RowRed", "Input row name",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = SelectedRow?.Name,
                        DialogResultOnCancel = MessageDialogResult.Negative

                    });
        } //TODO: add some logic preventing empty name

        public void Initialize(ViewRequest viewRequest)
        {
            shell.AddVMCommand("Edit", "Add tiket", "CreateTiketCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.W);

            shell.AddVMCommand("Edit", "Add column", "CreateColumnCommand", this);
            shell.AddVMCommand("Edit", "Add row", "CreateRowCommand", this);

            shell.AddVMCommand("Edit", "Normalize Grid", "NormalizeGridCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.G);

            shell.AddVMCommand("Boards", "Add board", "AddBoardCommand", this)
                .SetHotKey(ModifierKeys.Control | ModifierKeys.Shift, Key.N);

            shell.AddVMCommand("Boards", "Next board", "NextBoardCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.Q);

            var request = viewRequest as BoardViewRequest;

            scope = request.Scope;

            Observable.FromAsync(() => scope.GetAllBoardsInFileAsync())
                .ObserveOnDispatcher()
                .Subscribe(boards =>
                {
                    BoardsInFile.PublishCollection(boards);

                    foreach (var brd in boards)
                        shell.AddInstanceCommand("Boards", brd.Name, "SelectBoardCommand", this);

                    CurrentBoard = !string.IsNullOrEmpty(request.NeededBoardName)
                        ? BoardsInFile.First(board => board.Name == request.NeededBoardName)
                        : BoardsInFile.First();
                });
        }
    }//end of class
}
