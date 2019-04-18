using System.Collections.Generic;

namespace Kamban.Repository
{
    public interface IRepository
    {
        void Initialize(string uri);

        RowInfo CreateOrUpdateRow(RowInfo row);
        ColumnInfo CreateOrUpdateColumn(ColumnInfo column);
        Issue CreateOrUpdateIssue(Issue issue);
        BoardInfo CreateOrUpdateBoardInfo(BoardInfo info);

        List<ColumnInfo> GetAllColumns();
        List<RowInfo> GetAllRows();

        List<Issue> GetIssuesByBoardId(int boardId);
        List<RowInfo> GetRows(int boardId);
        List<ColumnInfo> GetColumns(int boardId);
        Issue GetIssue(int issueId);
        List<BoardInfo> GetAllBoardsInFile();

        void DeleteRow(int rowId);
        void DeleteColumn(int columnId);
        void DeleteIssue(int issueId);
        void DeleteBoard(int boardId);
    }
}
