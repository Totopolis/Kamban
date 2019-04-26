using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DynamicData;
using Kamban.Repository;
using Monik.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.Core
{
    public class BoxViewModel : ReactiveObject
    {
        [Reactive] public string Uri { get; set; }
        [Reactive] public bool Loaded { get; set; }

        [Reactive] public string Title { get; set; }
        [Reactive] public string Path { get; set; }
        [Reactive] public string SizeOf { get; set; }
        [Reactive] public DateTime LastEdit { get; set; }
        [Reactive] public int TotalTickets { get; set; }
        [Reactive] public string BoardList { get; set; }

        [Reactive] public SourceList<ColumnViewModel> Columns { get; set; }
        [Reactive] public SourceList<RowViewModel> Rows { get; set; }
        [Reactive] public SourceList<BoardViewModel> Boards { get; set; }

        public IObservable<bool> BoardsCountMoreOne { get; set; }

        [Reactive] public SourceList<CardViewModel> Cards { get; set; }

        private readonly IMonik mon;
        private readonly IMapper mapper;

        public BoxViewModel(IMonik monik, IMapper mapper)
        {
            this.mon = monik;
            this.mapper = mapper;

            Columns = new SourceList<ColumnViewModel>();
            Rows = new SourceList<RowViewModel>();

            Boards = new SourceList<BoardViewModel>();
            Cards = new SourceList<CardViewModel>();

            Cards
                .Connect()
                .AutoRefresh()
                .Subscribe(x => TotalTickets = Cards.Count);

            BoardsCountMoreOne = Boards
                .Connect()
                .AutoRefresh()
                .Select(x => Boards.Count > 1);

            Boards
                .Connect()
                .AutoRefresh()
                .Subscribe(bvm =>
                {
                    var lst = Boards.Items.Select(x => x.Name).ToList();
                    var str = string.Join(",", lst);
                    BoardList = str;
                });
        }

        public void Connect(IRepository repo)
        {
            ///////////////////
            // Boards AutoSaver
            ///////////////////
            Boards
                .Connect()
                .WhenAnyPropertyChanged("Name", "Modified")
                .Subscribe(async bvm =>
                {
                    mon.LogicVerbose($"AppModel.Boards.ItemChanged {bvm.Id}::{bvm.Name}::{bvm.Modified}");
                    var bi = mapper.Map<BoardViewModel, Board>(bvm);
                    await repo.CreateOrUpdateBoard(bi);
                });

            Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board add {bvm.Id}::{bvm.Name}");

                        var bi = mapper.Map<BoardViewModel, Board>(bvm);
                        await repo.CreateOrUpdateBoard(bi);

                        bvm.Id = bi.Id;
                    }));

            Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board remove {bvm.Id}::{bvm.Name}");

                        await repo.DeleteBoard(bvm.Id);
                    }));

            ////////////////////
            // Columns AutoSaver
            ////////////////////
            Columns
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(async cvm =>
                {
                    mon.LogicVerbose($"AppModel.Columns.ItemChanged {cvm.Id}::{cvm.Name}::{cvm.Order}");
                    var ci = mapper.Map<ColumnViewModel, Column>(cvm);
                    await repo.CreateOrUpdateColumn(ci);
                });

            Columns
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        mon.LogicVerbose($"AppModel.Column add {cvm.Id}::{cvm.Name}::{cvm.Order}");

                        var ci = mapper.Map<ColumnViewModel, Column>(cvm);
                        await repo.CreateOrUpdateColumn(ci);

                        cvm.Id = ci.Id;
                    }));

            Columns
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm => await repo.DeleteColumn(cvm.Id)));

            /////////////////
            // Rows AutoSaver
            /////////////////
            Rows
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(async rvm =>
                {
                    mon.LogicVerbose($"AppModel.Rows.ItemChanged {rvm.Id}::{rvm.Name}::{rvm.Order}");
                    var row = mapper.Map<RowViewModel, Row>(rvm);
                    await repo.CreateOrUpdateRow(row);
                });

            Rows
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async rvm =>
                    {
                        mon.LogicVerbose($"AppModel.Row add {rvm.Id}::{rvm.Name}::{rvm.Order}");

                        var ri = mapper.Map<RowViewModel, Row>(rvm);
                        await repo.CreateOrUpdateRow(ri);

                        rvm.Id = ri.Id;
                    }));

            Rows
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async rvm => await repo.DeleteRow(rvm.Id)));

            //////////////////
            // Cards AutoSaver
            //////////////////
            Cards
                .Connect()
                .WhenAnyPropertyChanged("Header", "Color", "ColumnDeterminant", "RowDeterminant",
                    "Order", "Body", "Modified", "BoardId")
                .Subscribe(async cvm =>
                {
                    mon.LogicVerbose("AppModel.Cards.WhenAnyPropertyChanged");
                    var iss = mapper.Map<CardViewModel, Card>(cvm);
                    await repo.CreateOrUpdateCard(iss);
                });

            Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        mon.LogicVerbose("AppModel.Cards add");
                        var iss = mapper.Map<CardViewModel, Card>(cvm);
                        await repo.CreateOrUpdateCard(iss);

                        cvm.Id = iss.Id;
                    }));

            Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        mon.LogicVerbose("AppModel.Cards remove");
                        await repo.DeleteCard(cvm.Id);
                    }));
        }

        public async Task Load(IRepository repo)
        {
            var cardsTask = repo.GetAllCards();
            var rowsTask = repo.GetAllRows();
            var columnsTask = repo.GetAllColumns();
            var boardsTask = repo.GetAllBoards();

            var cards = await cardsTask;
            var columns = await columnsTask;
            var rows = await rowsTask;
            var boards = await boardsTask;

            Cards.AddRange(cards.Select(x => mapper.Map<Card, CardViewModel>(x)));
            Columns.AddRange(columns.Select(x => mapper.Map<Column, ColumnViewModel>(x)));
            Rows.AddRange(rows.Select(x => mapper.Map<Row, RowViewModel>(x)));
            Boards.AddRange(boards.Select(x => mapper.Map<Board, BoardViewModel>(x)));

            Loaded = true;
        }
    }
}
