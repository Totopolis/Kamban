using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public interface IRepository
    {
        Task<Issue> CreateOrUpdateIssue(Issue issue);
        Task<RowInfo> CreateOrUpdateRow(RowInfo row);
        Task<ColumnInfo> CreateOrUpdateColumn(ColumnInfo column);        
        Task<BoardInfo> CreateOrUpdateBoardInfo(BoardInfo info);

        Task<List<Issue>> GetAllIssues();
        Task<List<RowInfo>> GetAllRows();
        Task<List<ColumnInfo>> GetAllColumns();
        Task<List<BoardInfo>> GetAllBoards();

        Task<List<Issue>> GetIssues(int boardId);
        Task<List<RowInfo>> GetRows(int boardId);
        Task<List<ColumnInfo>> GetColumns(int boardId);

        Task<Issue> GetIssue(int issueId);

        Task DeleteRow(int rowId);
        Task DeleteColumn(int columnId);
        Task DeleteIssue(int issueId);
        Task DeleteBoard(int boardId);
    }
}
