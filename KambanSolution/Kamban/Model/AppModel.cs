using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ui.Wpf.Common;
using Autofac;
using DynamicData;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using System.Threading.Tasks;
using AutoMapper;
using System.Reactive.Linq;

namespace Kamban.Model
{
    public interface IAppModel
    {
        ReadOnlyObservableCollection<DbViewModel> Dbs { get; }
        IObservable<bool> DbsCountMoreZero { get; }
        IObservable<bool> DbsCountMoreOne { get; }

        Task<DbViewModel> CreateDb(string uri);
        Task<DbViewModel> LoadDb(string uri);
        void RemoveDb(string uri);

        IProjectService GetProjectService(string uri);
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;
        private readonly IMapper mapper;
        private SourceList<DbViewModel> dbList;

        public AppModel(IShell shell, IMapper mp)
        {
            this.shell = shell;
            mapper = mp;

            dbList = new SourceList<DbViewModel>();

            dbList
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<DbViewModel>.Descending(x => x.LastAccess))
                .Bind(out ReadOnlyObservableCollection<DbViewModel> temp)
                .Subscribe();

            Dbs = temp;

            DbsCountMoreZero = dbList
                .Connect()
                .AutoRefresh()
                .Select(x => x.Count > 0);

            DbsCountMoreOne = dbList
                .Connect()
                .AutoRefresh()
                .Select(x => x.Count > 1);
        }

        public ReadOnlyObservableCollection<DbViewModel> Dbs { get; private set; }
        public IObservable<bool> DbsCountMoreZero { get; private set; }
        public IObservable<bool> DbsCountMoreOne { get; private set; }

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

            return db;
        }

        public async Task<DbViewModel> LoadDb(string uri)
        {
            var db = dbList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (db == null)
            {
                db = new DbViewModel { Uri = uri };

                if (!File.Exists(db.Uri))
                    return db;

                try
                {
                    db.LastAccess = File.GetLastWriteTime(db.Uri);

                    var prj = GetProjectService(db.Uri);

                    var columns = await prj.GetAllColumns();
                    var rows = await prj.GetAllRows();
                    var boards = await prj.GetAllBoardsInFileAsync();

                    db.Columns.AddRange(columns.Select(x => mapper.Map<ColumnInfo, ColumnViewModel>(x)));
                    db.Rows.AddRange(rows.Select(x => mapper.Map<RowInfo, RowViewModel>(x)));
                    db.Boards.AddRange(boards.Select(x => mapper.Map<BoardInfo, BoardViewModel>(x)));

                    db.TotalTickets = 0;
                    foreach (var brd in boards)
                        db.TotalTickets += (await prj.GetIssuesByBoardIdAsync(brd.Id)).Count();

                    db.Loaded = true;
                }
                // Skip broken file
                catch { }

                dbList.Add(db);
            }

            return db;
        }

        public void RemoveDb(string uri)
        {
            var db = dbList.Items.Where(x => x.Uri == uri).FirstOrDefault();

            if (db != null)
                dbList.Remove(db);
        }

        public IProjectService GetProjectService(string uri)
        {
            var scope = shell
                .Container
                .Resolve<IProjectService>(new NamedParameter("uri", uri));

            return scope;
        }
    }//end of class
}
