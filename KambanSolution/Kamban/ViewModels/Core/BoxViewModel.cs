using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DynamicData;
using Kamban.Extensions;
using Kamban.Repository;
using Kamban.Repository.Models;
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
        [Reactive] public int TotalTickets { get; set; }
        [Reactive] public string BoardList { get; set; }

        [Reactive] public SourceList<ColumnViewModel> Columns { get; set; }
        [Reactive] public SourceList<RowViewModel> Rows { get; set; }
        [Reactive] public SourceList<BoardViewModel> Boards { get; set; }

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

        public void Connect(ISaveRepository repo)
        {
            ///////////////////
            // Boards AutoSaver
            ///////////////////
            var boardsLocker = new SemaphoreSlim(1, 1);
            var boardsChanges = Boards.Connect().Publish();
            SubscribeChanged(boardsChanges, boardsLocker,
                async bvm =>
                {
                    mon.LogicVerbose($"Box.Boards.ItemChanged {bvm.Id}::{bvm.Name}::{bvm.Modified}");
                    var bi = mapper.Map<BoardViewModel, Board>(bvm);
                    await repo.CreateOrUpdateBoard(bi);
                });

            SubscribeAdded(boardsChanges, boardsLocker,
                async bvm =>
                {
                    mon.LogicVerbose($"Box.Boards.Add {bvm.Id}::{bvm.Name}");

                    var bi = mapper.Map<BoardViewModel, Board>(bvm);
                    bi = await repo.CreateOrUpdateBoard(bi);

                    bvm.Id = bi.Id;
                });

            SubscribeRemoved(boardsChanges, boardsLocker,
                async bvm =>
                {
                    mon.LogicVerbose($"Box.Boards.Remove {bvm.Id}::{bvm.Name}");

                    await repo.DeleteBoard(bvm.Id);
                });

            boardsChanges.Connect();

            ////////////////////
            // Columns AutoSaver
            ////////////////////
            var columnsLocker = new SemaphoreSlim(1, 1);
            var columnsChanges = Columns.Connect().Publish();
            SubscribeChanged(columnsChanges, columnsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Columns.ItemChanged {cvm.Id}::{cvm.Name}::{cvm.Order}");
                    var ci = mapper.Map<ColumnViewModel, Column>(cvm);
                    await repo.CreateOrUpdateColumn(ci);
                });

            SubscribeAdded(columnsChanges, columnsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Columns.Add {cvm.Id}::{cvm.Name}::{cvm.Order}");

                    var ci = mapper.Map<ColumnViewModel, Column>(cvm);
                    ci = await repo.CreateOrUpdateColumn(ci);

                    cvm.Id = ci.Id;
                });

            SubscribeRemoved(columnsChanges, columnsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Columns.Remove {cvm.Id}::{cvm.Name}::{cvm.Order}");

                    await repo.DeleteColumn(cvm.Id);
                });


            columnsChanges.Connect();

            /////////////////
            // Rows AutoSaver
            /////////////////
            var rowsLocker = new SemaphoreSlim(1, 1);
            var rowsChanges = Rows.Connect().Publish();
            SubscribeChanged(rowsChanges, rowsLocker,
                async rvm =>
                {
                    mon.LogicVerbose($"Box.Rows.ItemChanged {rvm.Id}::{rvm.Name}::{rvm.Order}");
                    var row = mapper.Map<RowViewModel, Row>(rvm);
                    await repo.CreateOrUpdateRow(row);
                });

            SubscribeAdded(rowsChanges, rowsLocker,
                async rvm =>
                {
                    mon.LogicVerbose($"Box.Rows.Add {rvm.Id}::{rvm.Name}::{rvm.Order}");

                    var ri = mapper.Map<RowViewModel, Row>(rvm);
                    ri = await repo.CreateOrUpdateRow(ri);

                    rvm.Id = ri.Id;
                });

            SubscribeRemoved(rowsChanges, rowsLocker,
                async rvm =>
                {
                    mon.LogicVerbose($"Box.Rows.Remove {rvm.Id}::{rvm.Name}::{rvm.Order}");

                    await repo.DeleteRow(rvm.Id);
                });

            rowsChanges.Connect();

            //////////////////
            // Cards AutoSaver
            //////////////////
            var cardsLocker = new SemaphoreSlim(1, 1);
            var cardsChanges = Cards.Connect().Publish();
            SubscribeChanged(cardsChanges, cardsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Cards.ItemChanged {cvm.Id}::{cvm.Header}");
                    var iss = mapper.Map<CardViewModel, Card>(cvm);
                    await repo.CreateOrUpdateCard(iss);
                });

            SubscribeAdded(cardsChanges, cardsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Cards.Add {cvm.Id}::{cvm.Header}");
                    var ci = mapper.Map<CardViewModel, Card>(cvm);
                    ci = await repo.CreateOrUpdateCard(ci);

                    cvm.Id = ci.Id;
                });

            SubscribeRemoved(cardsChanges, cardsLocker,
                async cvm =>
                {
                    mon.LogicVerbose($"Box.Cards.Remove {cvm.Id}::{cvm.Header}");

                    await repo.DeleteCard(cvm.Id);
                });

            cardsChanges.Connect();
        }

        public async Task Load(ILoadRepository repo)
        {
            var box = await repo.Load();
            Load(box);
        }

        public void Load(Box box)
        {
            Cards.AddRange(box.Cards.Select(x => mapper.Map<Card, CardViewModel>(x)));
            Columns.AddRange(box.Columns.Select(x => mapper.Map<Column, ColumnViewModel>(x)));
            Rows.AddRange(box.Rows.Select(x => mapper.Map<Row, RowViewModel>(x)));
            Boards.AddRange(box.Boards.Select(x => mapper.Map<Board, BoardViewModel>(x)));
            Loaded = true;
        }

        private static IDisposable SubscribeChanged<T>(IObservable<IChangeSet<T>> x, SemaphoreSlim s,
            Func<T, Task> a)
            where T : INotifyPropertyChanged
        {
            return x.WhenAnyAutoSavePropertyChanged()
                .Select(cvm => Observable.FromAsync(async () =>
                {
                    await s.WaitAsync();
                    try
                    {
                        await a(cvm);
                    }
                    finally
                    {
                        s.Release();
                    }
                }))
                .Concat()
                .Subscribe();
        }

        private static IDisposable SubscribeAdded<T>(IObservable<IChangeSet<T>> x, SemaphoreSlim s, Func<T, Task> a)
            where T : INotifyPropertyChanged
        {
            return x.WhereReasonsAre(ListChangeReason.Add, ListChangeReason.AddRange)
                .Select(c => Observable.FromAsync(async () =>
                {
                    await s.WaitAsync();
                    try
                    {
                        var vms = c.SelectMany(q =>
                        {
                            var list = new List<T>();
                            if (q.Range != null)
                                list.AddRange(q.Range);
                            if (q.Item.Current != null)
                                list.Add(q.Item.Current);
                            return list;
                        });

                        foreach (var cvm in vms)
                            await a(cvm);
                    }
                    finally
                    {
                        s.Release();
                    }
                }))
                .Concat()
                .Subscribe();
        }

        private static IDisposable SubscribeRemoved<T>(IObservable<IChangeSet<T>> x, SemaphoreSlim s, Func<T, Task> a)
            where T : INotifyPropertyChanged
        {
            return x.WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(c => c
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        await s.WaitAsync();
                        try
                        {
                            await a(cvm);
                        }
                        finally
                        {
                            s.Release();
                        }
                    }));
        }
    }
}