using Kamban.SqliteLocalStorage.Entities;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public class LiteDbRepository : IRepository
    {
        private string fileName;

        public LiteDbRepository() { }

        public void Initialize(string uri)
        {
            fileName = uri;
        }

        private string Name<T>()
        {
            if (typeof(T) == typeof(RowInfo))
                return "rows";
            else
                if (typeof(T) == typeof(ColumnInfo))
                return "columns";
            else
                if (typeof(T) == typeof(BoardInfo))
                return "boards";
            else
                if (typeof(T) == typeof(LocalIssue))
                return "issues";

            throw new NotImplementedException();
        }

        private T Upsert<T>(T rec)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(fileName))
            {
                var rows = db.GetCollection<T>(Name<T>());
                rows.Upsert(rec);
                return rec;
            }
        }

        public RowInfo CreateOrUpdateRow(RowInfo row)
        {
            return Upsert(row);
        }

        public ColumnInfo CreateOrUpdateColumn(ColumnInfo column)
        {
            return Upsert(column);
        }

        public LocalIssue CreateOrUpdateIssue(LocalIssue issue)
        {
            return Upsert(issue);
        }

        public BoardInfo CreateOrUpdateBoardInfo(BoardInfo info)
        {
            return Upsert(info);
        }

        public List<LocalIssue> GetIssuesByBoardId(int boardId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var issues = db.GetCollection<LocalIssue>(Name<LocalIssue>());
                var results = issues.Find(x => x.BoardId == boardId);

                return results.ToList();
            }
        }

        public List<RowInfo> GetRows(int boardId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var rows = db.GetCollection<RowInfo>(Name<RowInfo>());
                var result = rows.Find(x => x.Board.Id == boardId);

                return result.ToList();
            }
        }

        public List<ColumnInfo> GetColumns(int boardId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var columns = db.GetCollection<ColumnInfo>(Name<ColumnInfo>());
                var result = columns.Find(x => x.Board.Id == boardId);

                return result.ToList();
            }
        }

        public LocalIssue GetIssue(int issueId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var issues = db.GetCollection<LocalIssue>(Name<LocalIssue>());
                var result = issues.Find(x => x.Id == issueId);

                return result.First();
            }
        }

        public List<BoardInfo> GetAllBoardsInFile()
        {
            using (var db = new LiteDatabase(fileName))
            {
                var boards = db.GetCollection<BoardInfo>(Name<BoardInfo>());
                var result = boards.FindAll();

                return result.ToList();
            }
        }

        public void DeleteRow(int rowId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var rows = db.GetCollection<RowInfo>(Name<RowInfo>());
                rows.Delete(x => x.Id == rowId);
            }
        }

        public void DeleteColumn(int columnId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var columns = db.GetCollection<ColumnInfo>(Name<ColumnInfo>());
                columns.Delete(x => x.Id == columnId);
            }
        }

        public void DeleteIssue(int issueId)
        {
            using (var db = new LiteDatabase(fileName))
            {
                var issues = db.GetCollection<LocalIssue>(Name<LocalIssue>());
                issues.Delete(x => x.Id == issueId);
            }
        }

    }//end of class
}
