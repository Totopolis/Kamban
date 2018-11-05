using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ui.Wpf.Common;
using Kamban.Repository;

namespace Kamban.Model
{
    /// LiteDb or Api access to board by url
    public interface IProjectService
    {
        string Uri { get; set; }

        Task<List<ColumnInfo>> GetAllColumns();
        Task<List<RowInfo>> GetAllRows();

        // Actual

        Task<List<BoardInfo>> GetAllBoardsInFileAsync();
        BoardInfo CreateOrUpdateBoardAsync(BoardInfo board);

        Task<List<ColumnInfo>> GetColumnsByBoardIdAsync(int boardId);
        Task<List<RowInfo>> GetRowsByBoardIdAsync(int boardId);
        Task<IEnumerable<Issue>> GetIssuesByBoardIdAsync(int boardId);

        void CreateOrUpdateColumnAsync(ColumnInfo column);
        void CreateOrUpdateRowAsync(RowInfo row);
        void CreateOrUpdateIssueAsync(Issue issue);

        void DeleteColumnAsync(int columnId);
        void DeleteRowAsync(int rowId);
        void DeleteIssueAsync(int issueId);
        void DeleteBoard(int boardId);

        // Obsolete

        RowInfo GetSelectedRow(string rowName);
        ColumnInfo GetSelectedColumn(string colName);

        Task<Issue> LoadOrCreateIssueAsync(int? issueId);
    }

    public class ProjectService : IProjectService
    {
        private readonly IRepository repo;

        private List<RowInfo> rows = new List<RowInfo>();
        private List<ColumnInfo> columns = new List<ColumnInfo>();

        public ProjectService(IShell shell, IRepository repository, string uri)
        {
            Uri = uri;
            repo = repository;
            repo.Initialize(uri);
        }

        public string Uri { get; set; }

        public async Task<List<ColumnInfo>> GetAllColumns()
        {
            return await Task.Run(() => repo.GetAllColumns());
        }

        public async Task<List<RowInfo>> GetAllRows()
        {
            return await Task.Run(() => repo.GetAllRows());
        }

        public async Task<List<BoardInfo>> GetAllBoardsInFileAsync()
        {
            return await Task.Run(() => repo.GetAllBoardsInFile());
        }

        public BoardInfo CreateOrUpdateBoardAsync(BoardInfo board)
        {
            return repo.CreateOrUpdateBoardInfo(board);
        }

        public async Task<List<ColumnInfo>> GetColumnsByBoardIdAsync(int boardId)
        {
            var columns = await Task.Run(() => repo.GetColumns(boardId));
            return columns;
        }

        public async Task<List<RowInfo>> GetRowsByBoardIdAsync(int boardId)
        {
            var rows = await Task.Run(() => repo.GetRows(boardId));
            return rows;
        }

        public async Task<IEnumerable<Issue>> GetIssuesByBoardIdAsync(int boardId)
        {
            return await Task.Run(() => repo.GetIssuesByBoardId(boardId));
        }

        public void CreateOrUpdateColumnAsync(ColumnInfo column)
        {
            repo.CreateOrUpdateColumn(column);
        }

        public void CreateOrUpdateRowAsync(RowInfo row)
        {
            repo.CreateOrUpdateRow(row);
        }

        public void CreateOrUpdateIssueAsync(Issue issue)
        {
            repo.CreateOrUpdateIssue(issue);
        }

        public void DeleteColumnAsync(int columnId)
        {
            repo.DeleteColumn(columnId);
        }

        public void DeleteRowAsync(int rowId)
        {
            repo.DeleteRow(rowId);
        }

        public void DeleteIssueAsync(int issueId)
        {
            repo.DeleteIssue(issueId);
        }

        public void DeleteBoard(int boardId)
        {
            repo.DeleteBoard(boardId);
        }

        #region Obsolete

        // TODO: remove obsolete

        public RowInfo GetSelectedRow(string rowName)
        {
            return rows.FirstOrDefault(r => r.Name == rowName);
        }

        public ColumnInfo GetSelectedColumn(string colName)
        {
            return columns.FirstOrDefault(c => c.Name == colName);
        }

        
        public async Task<Issue> LoadOrCreateIssueAsync(int? issueId)
        {
            var t = new Issue();
            if (issueId.HasValue)
                t = await Task.Run(() => repo.GetIssue(issueId.Value));

            return t;
        }

        #endregion

    }//end of class
}
