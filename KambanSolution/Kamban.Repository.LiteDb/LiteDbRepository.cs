using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kamban.Repository.LiteDb
{
    public class LiteDbRepository : IRepository
    {
        private readonly LiteDatabase db;

        public LiteDbRepository(string uri)
        {
            db = new LiteDatabase(uri);
        }

        public Task<RowInfo> CreateOrUpdateRow(RowInfo row)
        {
            return db.UpsertAsync(row);
        }

        public Task<ColumnInfo> CreateOrUpdateColumn(ColumnInfo column)
        {
            return db.UpsertAsync(column);
        }

        public Task<Issue> CreateOrUpdateIssue(Issue issue)
        {
            return db.UpsertAsync(issue);
        }

        public Task<BoardInfo> CreateOrUpdateBoardInfo(BoardInfo info)
        {
            return db.UpsertAsync(info);
        }

        public Task<List<Issue>> GetAllIssues()
        {
            return db.GetAllAsync<Issue>();
        }

        public Task<List<RowInfo>> GetAllRows()
        {
            return db.GetAllAsync<RowInfo>();
        }

        public Task<List<ColumnInfo>> GetAllColumns()
        {
            return db.GetAllAsync<ColumnInfo>();
        }

        public Task<List<BoardInfo>> GetAllBoards()
        {
            return db.GetAllAsync<BoardInfo>();
        }

        public Task<List<Issue>> GetIssues(int boardId)
        {
            return Task.Run(() =>
            {
                var issues = db.GetCollectionByType<Issue>();
                var results = issues.Find(x => x.BoardId == boardId);

                return results.ToList();
            });
        }

        public Task<List<RowInfo>> GetRows(int boardId)
        {
            return Task.Run(() =>
            {
                var rows = db.GetCollectionByType<RowInfo>();
                var result = rows.Find(x => x.BoardId == boardId);

                return result.ToList();
            });
        }

        public Task<List<ColumnInfo>> GetColumns(int boardId)
        {
            return Task.Run(() =>
            {
                var columns = db.GetCollectionByType<ColumnInfo>();
                var result = columns.Find(x => x.BoardId == boardId);

                return result.ToList();
            });
        }

        public Task<Issue> GetIssue(int issueId)
        {
            return Task.Run(() =>
            {
                var issues = db.GetCollectionByType<Issue>();
                var result = issues.Find(x => x.Id == issueId);

                return result.First();
            });
        }

        public Task DeleteRow(int rowId)
        {
            return Task.Run(() =>
            {
                var rows = db.GetCollectionByType<RowInfo>();
                rows.Delete(x => x.Id == rowId);
            });
        }

        public Task DeleteColumn(int columnId)
        {
            return Task.Run(() =>
            {
                var columns = db.GetCollectionByType<ColumnInfo>();
                columns.Delete(x => x.Id == columnId);
            });
        }

        public Task DeleteIssue(int issueId)
        {
            return Task.Run(() =>
            {
                var issues = db.GetCollectionByType<Issue>();
                issues.Delete(x => x.Id == issueId);
            });
        }

        public Task DeleteBoard(int boardId)
        {
            return Task.Run(() =>
            {
                var boards = db.GetCollectionByType<BoardInfo>();
                boards.Delete(x => x.Id == boardId);
            });
        }
    } //end of class
}