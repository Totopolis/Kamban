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
        ReadOnlyObservableCollection<DbViewModel> Dbs { get; }
        IObservable<bool> DbsCountMoreZero { get; }

        Task<DbViewModel> CreateDb(string uri);
        Task<DbViewModel> LoadDb(string uri);
        void RemoveDb(string uri);

        IProjectService GetProjectService(string uri);
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;
        private readonly IMapper mapper;
        private readonly IMonik mon;

        private SourceList<DbViewModel> dbList;

        public AppModel(IShell shell, IMapper mp, IMonik m)
        {
            this.shell = shell;
            mapper = mp;
            mon = m;

            dbList = new SourceList<DbViewModel>();

            dbList
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<DbViewModel>.Descending(x => x.LastEdit))
                .Bind(out ReadOnlyObservableCollection<DbViewModel> temp)
                .Subscribe();

            Dbs = temp;

            DbsCountMoreZero = dbList
                .Connect()
                .AutoRefresh()
                .Select(x => dbList.Count > 0);

            m.LogicVerbose("AppModel.ctor");
        }

        public ReadOnlyObservableCollection<DbViewModel> Dbs { get; private set; }
        public IObservable<bool> DbsCountMoreZero { get; private set; }

        public async Task<DbViewModel> CreateDb(string uri)
        {
            var db = dbList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (db != null)
                throw new Exception("Db already exists");

            if (File.Exists(uri))
                throw new Exception("File already exists");

            var prj = GetProjectService(uri);

            // init tables
            var columns = await prj.GetAllColumns();
            var rows = await prj.GetAllRows();
            var boards = await prj.GetAllBoardsInFileAsync();
            var cards = await prj.GetIssuesByBoardIdAsync(666);

            db = new DbViewModel { Uri = uri, Loaded = true };
            dbList.Add(db);
            EnableAutoSaver(db);

            return db;
        }

        public async Task<DbViewModel> LoadDb(string uri)
        {
            var db = dbList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (db == null)
            {
                db = new DbViewModel { Uri = uri };

                var fi = new FileInfo(uri);

                if (!fi.Exists)
                    return db;

                try
                {
                    db.Title = fi.Name;
                    db.LastEdit = File.GetLastWriteTime(db.Uri);
                    db.Path = fi.DirectoryName;
                    db.SizeOf = SizeSuffix(fi.Length);

                    var prj = GetProjectService(db.Uri);

                    var columns = await prj.GetAllColumns();
                    var rows = await prj.GetAllRows();
                    var boards = await prj.GetAllBoardsInFileAsync();

                    db.Columns.AddRange(columns.Select(x => mapper.Map<ColumnInfo, ColumnViewModel>(x)));
                    db.Rows.AddRange(rows.Select(x => mapper.Map<RowInfo, RowViewModel>(x)));
                    db.Boards.AddRange(boards.Select(x => mapper.Map<BoardInfo, BoardViewModel>(x)));

                    foreach (var brd in boards)
                    {
                        var issues = await prj.GetIssuesByBoardIdAsync(brd.Id);
                        db.Cards.AddRange(issues.Select(x => mapper.Map<Issue, CardViewModel>(x)));
                    }

                    db.Loaded = true;
                }
                // Skip broken file
                catch { }

                dbList.Add(db);
                EnableAutoSaver(db);
            }

            return db;
        }

        public void RemoveDb(string uri)
        {
            var db = dbList.Items.Where(x => x.Uri == uri).FirstOrDefault();

            if (db != null)
                dbList.Remove(db);
        }

        // TODO: remove ProjectService, use Repository directly

        public IProjectService GetProjectService(string uri)
        {
            var scope = shell
                .Container
                .Resolve<IProjectService>(new NamedParameter("uri", uri));

            return scope;
        }

        private void EnableAutoSaver(DbViewModel db)
        {
            var prj = GetProjectService(db.Uri);

            ///////////////////
            // Boards AutoSaver
            ///////////////////
            db.Boards
                .Connect()
                .WhenAnyPropertyChanged("Name", "Modified")
                .Subscribe(bvm =>
                {
                    mon.LogicVerbose($"AppModel.Boards.ItemChanged {bvm.Id}::{bvm.Name}::{bvm.Modified}");
                    var bi = mapper.Map<BoardViewModel, BoardInfo>(bvm);
                    prj.CreateOrUpdateBoardAsync(bi);
                });

            db.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board add {bvm.Id}::{bvm.Name}");

                        var bi = mapper.Map<BoardViewModel, BoardInfo>(bvm);
                        prj.CreateOrUpdateBoardAsync(bi);

                        bvm.Id = bi.Id;
                    }));

            db.Boards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(bvm =>
                    {
                        mon.LogicVerbose($"AppModel.Board remove {bvm.Id}::{bvm.Name}");

                        prj.DeleteBoard(bvm.Id);
                    }));

            ////////////////////
            // Columns AutoSaver
            ////////////////////
            db.Columns
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(cvm =>
                {
                    mon.LogicVerbose($"AppModel.Columns.ItemChanged {cvm.Id}::{cvm.Name}::{cvm.Order}");
                    var ci = mapper.Map<ColumnViewModel, ColumnInfo>(cvm);
                    prj.CreateOrUpdateColumnAsync(ci);
                });

            db.Columns
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(cvm =>
                    {
                        mon.LogicVerbose($"AppModel.Column add {cvm.Id}::{cvm.Name}::{cvm.Order}");

                        var ci = mapper.Map<ColumnViewModel, ColumnInfo>(cvm);
                        prj.CreateOrUpdateColumnAsync(ci);

                        cvm.Id = ci.Id;
                    }));

            db.Columns
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(cvm => prj.DeleteColumnAsync(cvm.Id)));

            /////////////////
            // Rows AutoSaver
            /////////////////
            db.Rows
                .Connect()
                //.AutoRefresh()
                .WhenAnyPropertyChanged("Name", "Order", "Size", "BoardId")
                .Subscribe(rvm =>
                {
                    mon.LogicVerbose($"AppModel.Rows.ItemChanged {rvm.Id}::{rvm.Name}::{rvm.Order}");
                    var row = mapper.Map<RowViewModel, RowInfo>(rvm);
                    prj.CreateOrUpdateRowAsync(row);
                });

            db.Rows
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(rvm =>
                    {
                        mon.LogicVerbose($"AppModel.Row add {rvm.Id}::{rvm.Name}::{rvm.Order}");

                        var ri = mapper.Map<RowViewModel, RowInfo>(rvm);
                        prj.CreateOrUpdateRowAsync(ri);

                        rvm.Id = ri.Id;
                    }));

            db.Rows
                .Connect()
                //.AutoRefresh()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(rvm => prj.DeleteRowAsync(rvm.Id)));

            //////////////////
            // Cards AutoSaver
            //////////////////
            db.Cards
                .Connect()
                .WhenAnyPropertyChanged("Header", "Color", "ColumnDeterminant", "RowDeterminant",
                    "Order", "Body", "Modified", "BoardId")
                .Subscribe(cvm =>
                {
                    mon.LogicVerbose("AppModel.Cards.WhenAnyPropertyChanged");
                    var iss = mapper.Map<CardViewModel, Issue>(cvm);
                    prj.CreateOrUpdateIssueAsync(iss);
                });

            db.Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Add)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(cvm =>
                  {
                      mon.LogicVerbose("AppModel.Cards add");
                      var iss = mapper.Map<CardViewModel, Issue>(cvm);
                      prj.CreateOrUpdateIssueAsync(iss);

                      cvm.Id = iss.Id;
                  }));

            db.Cards
                .Connect()
                .WhereReasonsAre(ListChangeReason.Remove)
                .Subscribe(x => x
                    .Select(q => q.Item.Current)
                    .ToList()
                    .ForEach(cvm =>
                    {
                        mon.LogicVerbose("AppModel.Cards remove");
                        prj.DeleteIssueAsync(cvm.Id);
                    }));
        }

        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
        }
    }//end of class
}
