using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AutoMapper;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using Kamban.ViewModels.Core;
using Kamban.ViewRequests;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using CardsObservableType = System.IObservable<DynamicData.IChangeSet<Kamban.ViewModels.Core.ICard>>;

namespace Kamban.ViewModels
{
    public partial class BoardEditViewModel : ViewModelBase, 
        IInitializableViewModel, IActivatableViewModel
    {
        private readonly IShell shell;
        private readonly IDialogCoordinator dialCoord;
        private readonly IMonik mon;
        private readonly IMapper mapper;
        
        private ReadOnlyObservableCollection<CardViewModel> cardList;

        public BoxViewModel Box { get; set; }

        [Reactive] public bool EnableMatrix { get; set; }
        [Reactive] public IMonik Monik { get; set; }

        [Reactive] public BoardViewModel CurrentBoard { get; set; }

        [Reactive] public ReadOnlyObservableCollection<ColumnViewModel> Columns { get; set; }
        [Reactive] public ReadOnlyObservableCollection<RowViewModel> Rows { get; set; }

        [Reactive] public CardsObservableType CardsObservable { get; set; }

        [Reactive] public ICard CardOfContextMenu { get; set; }

        public ReactiveCommand<ICard, Unit> CardClickCommand { get; set; }
        public ReactiveCommand<Unit, Unit> NormalizeGridCommand { get; set; }

        public ReactiveCommand<ICard, Unit> MoveCardCommand { get; set; }
        public ReactiveCommand<ICard, Unit> DeleteCardCommand { get; set; }

        [Reactive] public CardEditViewModel CardEditFlyout { get; set; }
        [Reactive] public CardMoveViewModel CardMoveFlyout { get; set; }

        [Reactive] public object HeadOfContextMenu { get; set; }

        public ReactiveCommand<IDim, Unit> HeadRenameCommand { get; set; }
        public ReactiveCommand<IDim, Unit> HeadDeleteCommand { get; set; }
        public ReactiveCommand<IDim, Unit> InsertHeadBeforeCommand { get; set; }
        public ReactiveCommand<IDim, Unit> InsertHeadAfterCommand { get; set; }

        [Reactive] public ReadOnlyObservableCollection<BoardViewModel> BoardsInFile { get; set; }

        public ReactiveCommand<Unit, Unit> CreateCardCommand { get; set; }
        public ReactiveCommand<(object column, object row), Unit> CellDoubleClickCommand { get; set; }

        public ReactiveCommand<Unit, Unit> CreateColumnCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CreateRowCommand { get; set; }

        public ReactiveCommand<Unit, Unit> AddBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> PrevBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> NextBoardCommand { get; set; }
        public ReactiveCommand<Unit, Unit> RenameBoardCommand { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> DeleteBoardCommand { get; set; }
        public ReactiveCommand<object, Unit> SelectBoardCommand { get; set; }

        public BoardEditViewModel(IShell shell, IDialogCoordinator dc, IMonik m, IMapper mp)
        {
            this.shell = shell;
            dialCoord = dc;
            mon = m;
            mapper = mp;

            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.ctor started");

            EnableMatrix = false;
            Monik = mon;

            Columns = null;
            Rows = null;
            cardList = null;
            CardsObservable = null;

            BoardsInFile = null;
            CardEditFlyout = new CardEditViewModel();
            CardMoveFlyout = new CardMoveViewModel(mapper);

            CardClickCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(CardEditFlyout, c));
            NormalizeGridCommand = ReactiveCommand.Create(() => { });

            MoveCardCommand = ReactiveCommand.Create<ICard>(c => ShowFlyout(CardMoveFlyout, c));

            DeleteCardCommand = ReactiveCommand
                .Create<ICard>(async card => await DeleteCardCommandExecuteAsync(card));

            HeadRenameCommand = ReactiveCommand
                .Create<IDim>(async head => await HeadRenameCommandExecute(head));

            HeadDeleteCommand = ReactiveCommand
                .Create<IDim>(async head => await HeadDeleteCommandExecute(head));

            InsertHeadBeforeCommand = ReactiveCommand
                .Create<IDim>(async head => await InsertHeadBeforeCommandExecute(head));

            InsertHeadAfterCommand = ReactiveCommand
                .Create<IDim>(async head => await InsertHeadAfterCommandExecute(head));

            CreateCardCommand = ReactiveCommand.Create(() => ShowFlyout(CardEditFlyout, null));

            CellDoubleClickCommand = ReactiveCommand.Create<(object column, object row)>(
                (tup) => ShowFlyout(CardEditFlyout, null, (int)tup.column, (int)tup.row));

            CreateColumnCommand = ReactiveCommand.CreateFromTask(() =>
                InsertHeadAfterCommandExecute(Columns.Last()));

            CreateRowCommand = ReactiveCommand.CreateFromTask(() =>
                InsertHeadAfterCommandExecute(Rows.Last()));

            AddBoardCommand = ReactiveCommand.Create(() =>
            {
                this.shell.ShowView<WizardView>(new WizardViewRequest
                {
                    ViewId = $"Creating new board in {Box.Uri}",
                    Uri = Box.Uri
                });
            });

            RenameBoardCommand = ReactiveCommand.CreateFromTask(RenameBoardCommandExecute);

            SelectBoardCommand = ReactiveCommand
                .Create((object mi) =>
                {
                    mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(SelectBoardCommand)} executed");

                    string name = ((MenuItem)mi).Header as string;
                    CurrentBoard = BoardsInFile.First(x => x.Name == name);
                });

            this.ObservableForProperty(w => w.CurrentBoard)
                .Where(v => v != null)
                .ObserveOnDispatcher()
                .Subscribe(_ => OnBoardChanged());

            this.ObservableForProperty(w => w.CardEditFlyout.IsOpened)
                .Where(x => x.Value == false && CardEditFlyout.Result == CardEditResult.Created)
                .Subscribe(_ =>
                {
                    mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(CardEditFlyout)} closed and card will be created");

                    var card = CardEditFlyout.Card;
                    var targetCards = cardList
                        .Where(x => x.ColumnDeterminant == card.ColumnDeterminant
                            && x.RowDeterminant == card.RowDeterminant)
                        .ToList();

                    card.Order = !targetCards.Any() ? 0 :
                        targetCards.Max(x => x.Order) + 10;

                    Box.Cards.Add(card);
                });

            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.ctor finished");
        }

        private void OnBoardChanged()
        {
            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(CurrentBoard)} changed");

            BoardsInFile.ToList().ForEach(x => x.IsChecked = false);

            CurrentBoard.IsChecked = true;

            EnableMatrix = false;

            Box.Columns
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Sort(SortExpressionComparer<ColumnViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<ColumnViewModel> temp3)
                .Subscribe();

            Columns = temp3;

            Box.Rows
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Sort(SortExpressionComparer<RowViewModel>.Ascending(x => x.Order))
                .Bind(out ReadOnlyObservableCollection<RowViewModel> temp4)
                .Subscribe();

            Rows = temp4;

            Title = CurrentBoard.Name;
            FullTitle = Box.Uri;

            CardsObservable = Box.Cards
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Transform(x => x as ICard);

            Box.Cards
                .Connect()
                .AutoRefresh()
                .Filter(x => x.BoardId == CurrentBoard.Id)
                .Bind(out ReadOnlyObservableCollection<CardViewModel> temp5)
                .Subscribe();

            cardList = temp5;

            //var toDel = issues.Where(x => x.ColumnId == 0 || x.RowId == 0).ToArray();
            //foreach (var it in toDel)
            //    scope.DeleteIssueAsync(it.Id);

            EnableMatrix = true;
        }

        private void ShowFlyout(IInitializableViewModel vm, ICard cvm, int column = 0, int row = 0)
        {
            vm.Initialize(new CardViewRequest
            {
                Box = this.Box,
                ColumnId = column,
                RowId = row,
                Card = cvm as CardViewModel,
                BoardVm = this,
                Board = CurrentBoard
            });
        }

        public void Initialize(ViewRequest viewRequest)
        {
            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(Initialize)} started");

            shell.AddVMCommand("Edit", "Add Card", nameof(CreateCardCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.W);

            shell.AddVMCommand("Edit", "Add Column", nameof(CreateColumnCommand), this);
            shell.AddVMCommand("Edit", "Add Row", nameof(CreateRowCommand), this, true);

            shell.AddVMCommand("Edit", "Normalize Grid", nameof(NormalizeGridCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.G);

            shell.AddVMCommand("Boards", "Add board", nameof(AddBoardCommand), this)
                .SetHotKey(ModifierKeys.Control | ModifierKeys.Shift, Key.N);

            shell.AddVMCommand("Boards", "Rename board", nameof(RenameBoardCommand), this);
            shell.AddVMCommand("Boards", "Delete board", nameof(DeleteBoardCommand), this);

            shell.AddVMCommand("Boards", "Prev board", nameof(PrevBoardCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.Q);

            shell.AddVMCommand("Boards", "Next board", nameof(NextBoardCommand), this, true)
                .SetHotKey(ModifierKeys.Control, Key.E);

            var request = (BoardViewRequest) viewRequest;

            Box = request.Box;

            var boardsChanges = Box.Boards.Connect().Publish();

            boardsChanges
                .AutoRefresh()
                .Sort(SortExpressionComparer<BoardViewModel>.Ascending(t => t.Created))
                .Bind(out var temp)
                .Subscribe();

            BoardsInFile = temp;

            boardsChanges
                .WhereReasonsAre(ListChangeReason.Add)
                .Select(x => x.Select(q => q.Item.Current).First())
                .Subscribe(bvm => bvm.MenuCommand = shell.AddInstanceCommand("Boards", bvm.Name, nameof(SelectBoardCommand), this));

            boardsChanges
                .WhereReasonsAre(ListChangeReason.Remove)
                .Select(x => x.Select(q => q.Item.Current).First())
                .Subscribe(bvm => shell.RemoveCommand(bvm.MenuCommand));

            var boxHasMoreThanOneBoard = boardsChanges
                .Count()
                .Select(x => x > 1);

            DeleteBoardCommand = ReactiveCommand.CreateFromTask(DeleteBoardCommandExecute, boxHasMoreThanOneBoard);

            PrevBoardCommand = ReactiveCommand.Create(() =>
            {
                var idx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = idx > 0 ?
                    BoardsInFile[idx - 1] :
                    BoardsInFile[BoardsInFile.Count - 1];
            }, boxHasMoreThanOneBoard);

            NextBoardCommand = ReactiveCommand.Create(() =>
            {
                var idx = BoardsInFile.IndexOf(CurrentBoard);

                CurrentBoard = idx < BoardsInFile.Count - 1 ?
                    BoardsInFile[idx + 1] :
                    BoardsInFile[0];
            }, boxHasMoreThanOneBoard);

            boardsChanges.Connect();

            BoardsInFile
                .ToList()
                .ForEach(x => x.MenuCommand = shell.AddInstanceCommand("Boards", x.Name, nameof(SelectBoardCommand), this));


            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(Initialize)} finished");
        }

        public void Activate(ViewRequest viewRequest)
        {
            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(Activate)} started");

            var request = viewRequest as BoardViewRequest;
            CurrentBoard = request?.Board ?? BoardsInFile.FirstOrDefault();

            mon.LogicVerbose($"{nameof(BoardEditViewModel)}.{nameof(Activate)} finished");
        }
    }//end of class
}
