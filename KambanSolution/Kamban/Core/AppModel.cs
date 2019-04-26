using Autofac;
using AutoMapper;
using DynamicData;
using DynamicData.Binding;
using Kamban.Model;
using Kamban.Repository;
using Monik.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ui.Wpf.Common;

namespace Kamban.Core
{
    public interface IAppModel
    {
        ReadOnlyObservableCollection<BoxViewModel> Boxes { get; }
        IObservable<bool> Exist { get; }

        Task<BoxViewModel> Create(string uri);
        Task<BoxViewModel> Load(string uri);
        void Remove(string uri);

        IProjectService GetProjectService(string uri);
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;
        private readonly IMapper mapper;
        private readonly IMonik mon;

        private readonly SourceList<BoxViewModel> boxesList;

        public AppModel(IShell shell, IMapper mp, IMonik m)
        {
            this.shell = shell;
            mapper = mp;
            mon = m;

            boxesList = new SourceList<BoxViewModel>();

            boxesList
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<BoxViewModel>.Descending(x => x.LastEdit))
                .Bind(out ReadOnlyObservableCollection<BoxViewModel> temp)
                .Subscribe();

            Boxes = temp;

            Exist = boxesList
                .Connect()
                .AutoRefresh()
                .Select(x => boxesList.Count > 0);

            m.LogicVerbose("AppModel.ctor");
        }

        public ReadOnlyObservableCollection<BoxViewModel> Boxes { get; private set; }
        public IObservable<bool> Exist { get; private set; }

        public Task<BoxViewModel> Create(string uri)
        {
            var box = boxesList.Items.FirstOrDefault(x => x.Uri == uri);
            if (box != null)
                throw new Exception("Already created");

            if (File.Exists(uri))
                throw new Exception("File already exists");

            box = new BoxViewModel {Uri = uri, Loaded = true};
            boxesList.Add(box);
            EnableAutoSaver(box);

            return Task.FromResult(box);
        }

        public async Task<BoxViewModel> Load(string uri)
        {
            var box = boxesList.Items.FirstOrDefault(x => x.Uri == uri);
            if (box == null)
            {
                box = new BoxViewModel {Uri = uri};

                var fi = new FileInfo(uri);

                if (!fi.Exists)
                    return box;

                try
                {
                    box.Title = fi.Name;
                    box.LastEdit = File.GetLastWriteTime(box.Uri);
                    box.Path = fi.DirectoryName;
                    box.SizeOf = SizeSuffix(fi.Length);

                    var prj = GetProjectService(box.Uri);

                    var cardsTask = prj.Repository.GetAllCards();
                    var rowsTask = prj.Repository.GetAllRows();
                    var columnsTask = prj.Repository.GetAllColumns();
                    var boardsTask = prj.Repository.GetAllBoards();

                    var cards = await cardsTask;
                    var columns = await columnsTask;
                    var rows = await rowsTask;
                    var boards = await boardsTask;

                    box.Cards.AddRange(cards.Select(x => mapper.Map<Card, CardViewModel>(x)));
                    box.Columns.AddRange(columns.Select(x => mapper.Map<Column, ColumnViewModel>(x)));
                    box.Rows.AddRange(rows.Select(x => mapper.Map<Row, RowViewModel>(x)));
                    box.Boards.AddRange(boards.Select(x => mapper.Map<Board, BoardViewModel>(x)));

                    box.Loaded = true;
                }
                // Skip broken file
                catch
                {
                }

                boxesList.Add(box);
                EnableAutoSaver(box);
            }

            return box;
        }

        public void Remove(string uri)
        {
            var box = boxesList.Items.FirstOrDefault(x => x.Uri == uri);

            if (box != null)
                boxesList.Remove(box);
        }

        // TODO: remove ProjectService, use Repository directly

        public IProjectService GetProjectService(string uri)
        {
            var scope = shell
                .Container
                .Resolve<IProjectService>(new NamedParameter("uri", uri));

            return scope;
        }

        private void EnableAutoSaver(BoxViewModel box)
        {
            var prj = GetProjectService(box.Uri);

            ///////////////////
            // Boards AutoSaver
            ///////////////////
            box.Boards
                .Connect()
                .WhenAnyPropertyChanged("Name", "Modified")
                .Subscribe(async bvm =>
                {
                    mon.LogicVerbose($"AppModel.Boards.ItemChanged {bvm.Id}::{bvm.Name}::{bvm.Modified}");
                    var bi = mapper.Map<BoardViewModel, Board>(bvm);
                    await prj.Repository.CreateOrUpdateBoard(bi);
                });

            box.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board add {bvm.Id}::{bvm.Name}");

                        var bi = mapper.Map<BoardViewModel, Board>(bvm);
                        await prj.Repository.CreateOrUpdateBoard(bi);

                        bvm.Id = bi.Id;
                    }));

            box.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board remove {bvm.Id}::{bvm.Name}");

                        await prj.Repository.DeleteBoard(bvm.Id);
                    }));

            ////////////////////
            // Columns AutoSaver
            ////////////////////
            box.Columns
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(async cvm =>
                {
                    mon.LogicVerbose($"AppModel.Columns.ItemChanged {cvm.Id}::{cvm.Name}::{cvm.Order}");
                    var ci = mapper.Map<ColumnViewModel, Column>(cvm);
                    await prj.Repository.CreateOrUpdateColumn(ci);
                });

            box.Columns
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
                        await prj.Repository.CreateOrUpdateColumn(ci);

                        cvm.Id = ci.Id;
                    }));

            box.Columns
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm => await prj.Repository.DeleteColumn(cvm.Id)));

            /////////////////
            // Rows AutoSaver
            /////////////////
            box.Rows
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(async rvm =>
                {
                    mon.LogicVerbose($"AppModel.Rows.ItemChanged {rvm.Id}::{rvm.Name}::{rvm.Order}");
                    var row = mapper.Map<RowViewModel, Row>(rvm);
                    await prj.Repository.CreateOrUpdateRow(row);
                });

            box.Rows
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
                        await prj.Repository.CreateOrUpdateRow(ri);

                        rvm.Id = ri.Id;
                    }));

            box.Rows
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async rvm => await prj.Repository.DeleteRow(rvm.Id)));

            //////////////////
            // Cards AutoSaver
            //////////////////
            box.Cards
                .Connect()
                .WhenAnyPropertyChanged("Header", "Color", "ColumnDeterminant", "RowDeterminant",
                    "Order", "Body", "Modified", "BoardId")
                .Subscribe(async cvm =>
                {
                    mon.LogicVerbose("AppModel.Cards.WhenAnyPropertyChanged");
                    var iss = mapper.Map<CardViewModel, Card>(cvm);
                    await prj.Repository.CreateOrUpdateCard(iss);
                });

            box.Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        mon.LogicVerbose("AppModel.Cards add");
                        var iss = mapper.Map<CardViewModel, Card>(cvm);
                        await prj.Repository.CreateOrUpdateCard(iss);

                        cvm.Id = iss.Id;
                    }));

            box.Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(async cvm =>
                    {
                        mon.LogicVerbose("AppModel.Cards remove");
                        await prj.Repository.DeleteCard(cvm.Id);
                    }));
        }

        static readonly string[] SizeSuffixes =
            {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        private static string SizeSuffix(long value)
        {
            if (value < 0)
            {
                return "-" + SizeSuffix(-value);
            }

            var i = 0;
            var dValue = (decimal) value;
            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return $"{dValue:n1} {SizeSuffixes[i]}";
        }
    } //end of class
}