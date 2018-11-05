using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using AutoMapper;
using DynamicData;
using DynamicData.Binding;
using Kamban.MatrixControl;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public partial class BoardEditViewModel : ViewModelBase, 
        IInitializableViewModel, IActivatableViewModel
    {
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;
        private readonly IMonik mon;
        private readonly IMapper mapper;
        private readonly IAppModel appModel;

        private IProjectService prjService;

        private DbViewModel Db { get; set; }

        [Reactive] public bool EnableMatrix { get; set; }
        [Reactive] public IMonik Monik { get; set; }

        [Reactive] public BoardViewModel CurrentBoard { get; set; }

        [Reactive] public ReadOnlyObservableCollection<ColumnViewModel> Columns { get; set; }
        [Reactive] public ReadOnlyObservableCollection<RowViewModel> Rows { get; set; }

        // TODO: use global cards
        [Reactive] public SourceList<ICard> Cards { get; set; }

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

        [Reactive] public ReadOnlyObservableCollection<BoardViewModel> BoardsInFile { get; set; }

        public ReactiveCommand<Unit, Unit> CreateTiketCommand { get; set; }
        public ReactiveCommand<(object column, object row), Unit> CellDoubleClickCommand { get; set; }

        public ReactiveCommand<Unit, Unit> CreateColumnCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CreateRowCommand { get; set; }

        public ReactiveCommand<Unit, Unit> AddBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> PrevBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> NextBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RenameBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> DeleteBoardCommand { get; set; }
        public ReactiveCommand<object, Unit> SelectBoardCommand { get; set; }

        public BoardEditViewModel(IShell shell, IDialogCoordinator dc, IMonik m, IMapper mp, IAppModel am)
        {
            this.shell = shell;
            dialCoord = dc;
            mon = m;
            mapper = mp;
            appModel = am;

            mon.LogicVerbose("BoardViewModel.ctor started");

            EnableMatrix = false;
            Monik = mon;

            Columns = null;
            Rows = null;
            Cards = new SourceList<ICard>();

            BoardsInFile = null;
            IssueFlyout = new IssueViewModel();
            MoveFlyout = new MoveViewModel(mapper);

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

            CreateColumnCommand = ReactiveCommand.CreateFromTask(() =>
                InsertHeadAfterCommandExecute(Columns.Last()));

            CreateRowCommand = ReactiveCommand.CreateFromTask(() =>
                InsertHeadAfterCommandExecute(Rows.Last()));

            AddBoardCommand = ReactiveCommand.Create(() =>
            {
                this.shell.ShowView<WizardView>(new WizardViewRequest
                {
                    ViewId = $"Creating new board in {prjService.Uri}",
                    Uri = prjService.Uri
                });
            });

            RenameBoardCommand = ReactiveCommand.CreateFromTask(RenameBoardCommandExecute);

            SelectBoardCommand = ReactiveCommand
                .Create((object mi) =>
                {
                    mon.LogicVerbose("BoardViewModel.SelectBoardCommand executed");

                    string name = ((MenuItem)mi).Header as string;
                    CurrentBoard = BoardsInFile.Where(x => x.Name == name).First();
                });

            Cards
                .Connect()
                .Transform(x => x as CardViewModel)
                .WhenAnyPropertyChanged()
                .Subscribe(cvm =>
                {
                    mon.LogicVerbose("BoardViewModel.Cards.WhenAnyPropertyChanged");
                    var iss = mapper.Map<CardViewModel, Issue>(cvm);
                    prjService.CreateOrUpdateIssueAsync(iss);
                });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .ObserveOnDispatcher()
                .Subscribe(async _ => await OnBoardChanged());

            this.ObservableForProperty(w => w.IssueFlyout.IsOpened)
                .Where(x => x.Value == false && IssueFlyout.Result == IssueEditResult.Created)
                .Subscribe(_ =>
                {
                    mon.LogicVerbose("BoardViewModel.IssueFlyout closed and issue need create");

                    var card = IssueFlyout.Card;
                    var targetCards = Cards.Items
                        .Where(x => x.ColumnDeterminant == card.ColumnDeterminant
                            && x.RowDeterminant == card.RowDeterminant);

                    card.Order = targetCards.Count() == 0 ? 0 :
                        targetCards.Max(x => x.Order) + 10;

                    var iss = mapper.Map<CardViewModel, Issue>(card);
                    prjService.CreateOrUpdateIssueAsync(iss);
                    card.Id = iss.Id;

                    Cards.Add(card);
                });

            mon.LogicVerbose("BoardViewModel.ctor finished");
        }

        private async Task OnBoardChanged()
        {
            mon.LogicVerbose("BoardViewModel.CurrentBoard changed");

            BoardsInFile.ToList().ForEach(x => x.IsChecked = false);

            CurrentBoard.IsChecked = true;

            EnableMatrix = false;

            Db.Columns
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Sort(SortExpressionComparer<ColumnViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<ColumnViewModel> temp3)
                .Subscribe();

            Columns = temp3;

            Db.Rows
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Sort(SortExpressionComparer<RowViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<RowViewModel> temp4)
                .Subscribe();

            Rows = temp4;

            Title = CurrentBoard.Name;
            FullTitle = prjService.Uri;

            var issues = await prjService.GetIssuesByBoardIdAsync(CurrentBoard.Id);

            //var toDel = issues.Where(x => x.ColumnId == 0 || x.RowId == 0).ToArray();
            //foreach (var it in toDel)
            //    scope.DeleteIssueAsync(it.Id);

            Cards.ClearAndAddRange(issues.Select(x => mapper.Map<Issue, CardViewModel>(x)));

            // TODO: reimplement without EnableMatrix

            EnableMatrix = true;
        }

        private void ShowFlyout(IInitializableViewModel vm, ICard cvm, int column = 0, int row = 0)
        {
            vm.Initialize(new IssueViewRequest
            {
                Db = this.Db,
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
            mon.LogicVerbose("BoardViewModel.Initialize started");

            shell.AddVMCommand("Edit", "Add tiket", "CreateTiketCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.W);

            shell.AddVMCommand("Edit", "Add column", "CreateColumnCommand", this);
            shell.AddVMCommand("Edit", "Add row", "CreateRowCommand", this, true);

            shell.AddVMCommand("Edit", "Normalize Grid", "NormalizeGridCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.G);

            shell.AddVMCommand("Boards", "Add board", "AddBoardCommand", this)
                .SetHotKey(ModifierKeys.Control | ModifierKeys.Shift, Key.N);

            shell.AddVMCommand("Boards", "Rename board", "RenameBoardCommand", this);
            shell.AddVMCommand("Boards", "Delete board", "DeleteBoardCommand", this);

            shell.AddVMCommand("Boards", "Prev board", "PrevBoardCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.Q);

            shell.AddVMCommand("Boards", "Next board", "NextBoardCommand", this, true)
                .SetHotKey(ModifierKeys.Control, Key.E);

            var request = viewRequest as BoardViewRequest;

            Db = request.Db;

            DeleteBoardCommand = ReactiveCommand.CreateFromTask(DeleteBoardCommandExecute, Db.BoardsCountMoreOne);

            PrevBoardCommand = ReactiveCommand.Create(() =>
            {
                int indx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = indx > 0 ?
                    BoardsInFile[indx - 1] :
                    BoardsInFile[BoardsInFile.Count - 1];
            }, Db.BoardsCountMoreOne);

            NextBoardCommand = ReactiveCommand.Create(() =>
            {
                int indx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = indx < BoardsInFile.Count - 1 ?
                    BoardsInFile[indx + 1] :
                    BoardsInFile[0];
            }, Db.BoardsCountMoreOne);

            prjService = appModel.GetProjectService(request.Db.Uri);

            Db.Boards
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<BoardViewModel>.Ascending(t => t.Created))
                .Bind(out ReadOnlyObservableCollection<BoardViewModel> temp)
                .Subscribe();

            BoardsInFile = temp;

            BoardsInFile
                .ToList()
                .ForEach(x => x.MenuCommand = shell.AddInstanceCommand("Boards", x.Name, "SelectBoardCommand", this));

            Db.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Select(x => x.Select(q => q.Item.Current).First())
                .Subscribe(bvm => bvm.MenuCommand = shell.AddInstanceCommand("Boards", bvm.Name, "SelectBoardCommand", this));

            Db.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Select(x => x.Select(q => q.Item.Current).First())
                .Subscribe(bvm => shell.RemoveCommand(bvm.MenuCommand));

            mon.LogicVerbose("BoardViewModel.Initialize finished");
        }

        public void Activate(ViewRequest viewRequest)
        {
            mon.LogicVerbose("BoardViewModel.Activate started");

            var request = viewRequest as BoardViewRequest;
            CurrentBoard = request.Board ?? BoardsInFile.First();

            mon.LogicVerbose("BoardViewModel.Activate finished");
        }
    }//end of class
}
