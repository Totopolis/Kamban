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
        private IProjectService prjService;

        private readonly IDialogCoordinator dialogCoordinator = DialogCoordinator.Instance;

        // Actual

        public ReactiveList<IDim> Columns { get; set; }
        public ReactiveList<IDim> Rows { get; set; }
        public ReactiveList<ICard> Cards { get; set; }

        [Reactive] public ICard IssueOfContextMenu { get; set; }

        public ReactiveCommand<ICard, Unit> CardClickCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NormalizeGridCommand { get; set; }
        public ReactiveCommand<ICard, Unit> DropCardCommand { get; set; }

        public ReactiveCommand<ICard, Unit> MoveIssueCommand { get; set; }
        public ReactiveCommand<ICard, Unit> DeleteIssueCommand { get; set; }

        [Reactive] public IssueViewModel IssueFlyout { get; set; }
        [Reactive] public MoveViewModel MoveFlyout { get; set; }

        // Obsolete

        [Reactive] private RowInfo SelectedRow { get; set; }
        [Reactive] private ColumnInfo SelectedColumn { get; set; }

        [Reactive] public ReactiveList<BoardInfo> BoardsInFile { get; set; }
        [Reactive] public BoardInfo CurrentBoard { get; set; }

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
            IssueFlyout = new IssueViewModel();
            MoveFlyout = new MoveViewModel();

            CardClickCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(IssueFlyout, c));
            NormalizeGridCommand = ReactiveCommand.Create(() => { });
            DropCardCommand = ReactiveCommand.Create<ICard>(DropCardCommandExecute);

            MoveIssueCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(MoveFlyout, c));

            DeleteIssueCommand = ReactiveCommand
                .Create<ICard>(async card => await DeleteCardCommandExecuteAsync(card));

            UpdateHorizontalHeaderCommand = ReactiveCommand.Create<object>(async ob => await UpdateHorizontalHeader(ob));
            UpdateVerticalHeaderCommand = ReactiveCommand.Create<object>(async ob => await UpdateVerticalHeader(ob));

            CreateTiketCommand = ReactiveCommand.Create(() => ShowFlyout(IssueFlyout, null));

            CreateColumnCommand = ReactiveCommand
                .CreateFromTask(async () =>
               {
                   var newName = await ShowColumnNameInput();

                   if (!string.IsNullOrEmpty(newName))
                   {
                       var newColumn = new ColumnInfo { Name = newName, Board = CurrentBoard };
                       prjService.CreateOrUpdateColumnAsync(newColumn);
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
                       prjService.CreateOrUpdateRowAsync(newRow);
                   }

                   await RefreshContent();
               });

            AddBoardCommand = ReactiveCommand.Create(() =>
            {
                this.shell.ShowView<WizardView>(new WizardViewRequest
                {
                    ViewId = $"Creating new board in {prjService.Uri}",
                    InExistedFile = true,
                    Uri = prjService.Uri
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

                    prjService.CreateOrUpdateColumnAsync(ci);
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

                    prjService.CreateOrUpdateRowAsync(ri);
                });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .ObserveOnDispatcher()
                .Subscribe(async _ => await RefreshContent());

            this.ObservableForProperty(w => w.IssueFlyout.Result)
                .Where(x => x.Value == IssueEditResult.Created)
                .Subscribe(_ => Cards.Add(IssueFlyout.Card));

            this.ObservableForProperty(w => w.IssueFlyout.Result)
                .Where(x => x.Value == IssueEditResult.Deleted)
                .Subscribe(_ => Cards.Remove(IssueFlyout.Card));
        }

        private async Task DeleteCardCommandExecuteAsync(ICard cvm)
        {
            var ts = await dialogCoordinator.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete issue '{cvm.Header}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            prjService.DeleteIssueAsync(cvm.Id);
            Cards.Remove(cvm);
        }

        // !!! save at drag&drop !!!
        private void DropCardCommandExecute(ICard cvm)
        {
            var editedIssue = new Issue
            {
                Id = cvm.Id,
                Head = cvm.Header,
                ColumnId = (int)cvm.ColumnDeterminant,
                RowId = (int)cvm.RowDeterminant,
                Color = cvm.Color,
                Body = cvm.Body,
                Created = cvm.Created,
                Modified = cvm.Modified,
                BoardId = cvm.BoardId
            };

            prjService.CreateOrUpdateIssueAsync(editedIssue);
        }

        private async Task RefreshContent()
        {
            try
            {
                var columns = await prjService.GetColumnsByBoardIdAsync(CurrentBoard.Id);
                var rows = await prjService.GetRowsByBoardIdAsync(CurrentBoard.Id);
                var issues = await prjService.GetIssuesByBoardIdAsync(CurrentBoard.Id);

                //var toDel = issues.Where(x => x.ColumnId == 0 || x.RowId == 0).ToArray();
                //foreach (var it in toDel)
                //    scope.DeleteIssueAsync(it.Id);

                Columns.ChangeTrackingEnabled = false;
                Rows.ChangeTrackingEnabled = false;
                Cards.ChangeTrackingEnabled = false;

                Columns.Clear();
                Rows.Clear();
                Cards.Clear();

                Columns.AddRange(columns.Select(x => new ColumnViewModel(x)));
                Rows.AddRange(rows.Select(x => new RowViewModel(x)));
                Cards.AddRange(issues.Select(x => new CardViewModel(x)));

                Cards.ChangeTrackingEnabled = true;
                Rows.ChangeTrackingEnabled = true;
                Columns.ChangeTrackingEnabled = true;
            }
            catch(Exception ex)
            {
                Title = "RefreshContent: " + ex.Message;
            }
        }

        private void ShowFlyout(IInitializableViewModel vm, ICard cvm)
        {
            vm.Initialize(new IssueViewRequest
            {
                Card = cvm as CardViewModel,
                PrjService = prjService,
                Board = CurrentBoard
            });
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

        /*private void UpdateCard(object o)
        {
            var iss = o as Issue;

            if (iss != null)
                IssueViewModel.Initialize(new IssueViewRequest
                {
                    IssueId = iss.Id,
                    Scope = scope,
                    Board = CurrentBoard
                });

            if (o is Issue)
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
        }*/

        private async Task UpdateHorizontalHeader(object o)
        {
            var newName = await ShowColumnNameInput();

            var column = prjService.GetSelectedColumn(o.ToString());

            if (!string.IsNullOrEmpty(newName))
            {
                column.Name = newName;
                prjService.CreateOrUpdateColumnAsync(column);
            }

            await RefreshContent();
        }

        private async Task UpdateVerticalHeader(object o)
        {
            var newName = await ShowRowNameInput();

            var row = prjService.GetSelectedRow(o.ToString());

            if (!string.IsNullOrEmpty(newName))
            {
                row.Name = newName;
                prjService.CreateOrUpdateRowAsync(row);
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

            prjService = request.PrjService;

            Observable.FromAsync(() => prjService.GetAllBoardsInFileAsync())
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
