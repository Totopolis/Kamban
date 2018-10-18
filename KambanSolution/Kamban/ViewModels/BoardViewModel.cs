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

namespace Kamban.ViewModels
{
    public partial class BoardViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;

        private IProjectService prjService;

        [Reactive] public BoardInfo CurrentBoard { get; set; }

        public ReactiveList<IDim> Columns { get; set; }
        public ReactiveList<IDim> Rows { get; set; }
        public ReactiveList<ICard> Cards { get; set; }

        [Reactive] public ICard IssueOfContextMenu { get; set; }

        public ReactiveCommand<ICard, Unit> CardClickCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NormalizeGridCommand { get; set; }

        public ReactiveCommand<ICard, Unit> MoveIssueCommand { get; set; }
        public ReactiveCommand<ICard, Unit> DeleteIssueCommand { get; set; }

        [Reactive] public IssueViewModel IssueFlyout { get; set; }
        [Reactive] public MoveViewModel MoveFlyout { get; set; }

        [Reactive] public object HeadOfContextMenu { get; set; }

        public ReactiveCommand<IDim, Unit> HeadRenameCommand { get; set; }
        public ReactiveCommand<IDim, Unit> HeadDeleteCommand { get; set; }
        public ReactiveCommand<IDim, Unit> InsertHeadBeforeCommand { get; set; }
        public ReactiveCommand<IDim, Unit> InsertHeadAfterCommand { get; set; }

        [Reactive] private RowInfo SelectedRow { get; set; }
        [Reactive] private ColumnInfo SelectedColumn { get; set; }

        [Reactive] public ReactiveList<BoardInfo> BoardsInFile { get; set; }

        public ReactiveCommand<Unit, Unit> CreateTiketCommand { get; set; }
        public ReactiveCommand<(object column, object row), Unit> CellDoubleClickCommand { get; set; }

        public ReactiveCommand<Unit, Unit> AddBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> PrevBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NextBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RenameBoardCommand { get; set; }
        public ReactiveCommand<object, Unit> SelectBoardCommand { get; set; }

        

        public BoardViewModel(IShell shell, IDialogCoordinator dc)
        {
            this.shell = shell;
            dialCoord = dc;

            Columns = new ReactiveList<IDim>();
            Rows = new ReactiveList<IDim>();
            Cards = new ReactiveList<ICard>();

            BoardsInFile = new ReactiveList<BoardInfo>();
            IssueFlyout = new IssueViewModel();
            MoveFlyout = new MoveViewModel();

            CardClickCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(IssueFlyout, c));
            NormalizeGridCommand = ReactiveCommand.Create(() => { });

            MoveIssueCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(MoveFlyout, c));

            DeleteIssueCommand = ReactiveCommand
                .Create<ICard>(async card => await DeleteCardCommandExecuteAsync(card));

            HeadRenameCommand = ReactiveCommand
                .Create<IDim>(async head => await HeadRenameCommandExecute(head));

            HeadDeleteCommand = ReactiveCommand
                .Create<IDim>(async head => await HeadDeleteCommandExecute(head));

            InsertHeadBeforeCommand = ReactiveCommand
                .Create<IDim>(async head => await InsertHeadBeforeCommandExecute(head));

            InsertHeadAfterCommand = ReactiveCommand
                .Create<IDim>(async head => await InsertHeadAfterCommandExecute(head));

            CreateTiketCommand = ReactiveCommand.Create(() => ShowFlyout(IssueFlyout, null));

            CellDoubleClickCommand = ReactiveCommand.Create<(object column, object row)>(
                (tup) => ShowFlyout(IssueFlyout, null, (int)tup.column, (int)tup.row));

            AddBoardCommand = ReactiveCommand.Create(() =>
            {
                this.shell.ShowView<WizardView>(new WizardViewRequest
                {
                    ViewId = $"Creating new board in {prjService.Uri}",
                    InExistedFile = true,
                    Uri = prjService.Uri
                });
            });

            var prevNextCommandEnabled = this.BoardsInFile
                .CountChanged
                .Select(x => x > 1);
            
            PrevBoardCommand = ReactiveCommand.Create(() =>
            {
                int indx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = indx > 0 ?
                    BoardsInFile[indx - 1] :
                    BoardsInFile[BoardsInFile.Count - 1];
            }, prevNextCommandEnabled);

            NextBoardCommand = ReactiveCommand.Create(() =>
            {
                int indx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = indx < BoardsInFile.Count - 1 ?
                    BoardsInFile[indx + 1] :
                    BoardsInFile[0];
            }, prevNextCommandEnabled);

            RenameBoardCommand = ReactiveCommand.CreateFromTask(RenameBoardCommandExecute);

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
                    prjService.CreateOrUpdateColumnAsync(cvm.Info);
                });

            Rows
                .ItemChanged
                .Subscribe(ob =>
                {
                    var rvm = ob.Sender as RowViewModel;
                    prjService.CreateOrUpdateRowAsync(rvm.Info);
                });

            Cards
                .ItemChanged
                .Subscribe(ob =>
                {
                    var cvm = ob.Sender as CardViewModel;
                    prjService.CreateOrUpdateIssueAsync(cvm.Issue);
                });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .ObserveOnDispatcher()
                .Subscribe(async _ => await RefreshContent());

            this.ObservableForProperty(w => w.IssueFlyout.IsOpened)
                .Where(x => x.Value == false && IssueFlyout.Result == IssueEditResult.Created)
                .Subscribe(_ =>
                {
                    var card = IssueFlyout.Card;
                    prjService.CreateOrUpdateIssueAsync(card.Issue);
                    Cards.Add(card);
                });
        }
        
        private async Task RefreshContent()
        {
            try
            {
                Title = CurrentBoard.Name;
                FullTitle = prjService.Uri;

                var columns = await prjService.GetColumnsByBoardIdAsync(CurrentBoard.Id);
                columns.Sort((x, y) => x.Order.CompareTo(y.Order));

                var rows = await prjService.GetRowsByBoardIdAsync(CurrentBoard.Id);
                rows.Sort((x, y) => x.Order.CompareTo(y.Order));

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

        private void ShowFlyout(IInitializableViewModel vm, ICard cvm, int column = 0, int row = 0)
        {
            vm.Initialize(new IssueViewRequest
            {
                ColumnId = column,
                RowId = row,
                Card = cvm as CardViewModel,
                PrjService = prjService,
                BoardVM = this,
                Board = CurrentBoard
            });
        }

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

            shell.AddVMCommand("Boards", "Rename board", "RenameBoardCommand", this);

            shell.AddVMCommand("Boards", "Prev board", "PrevBoardCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.Q);

            shell.AddVMCommand("Boards", "Next board", "NextBoardCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.E);

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
