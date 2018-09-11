using Kamban.SqliteLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public interface IRepository
    {
        void Initialize(string uri);

        RowInfo CreateOrUpdateRow(RowInfo row);
        ColumnInfo CreateOrUpdateColumn(ColumnInfo column);
        LocalIssue CreateOrUpdateIssue(LocalIssue issue);
        BoardInfo CreateOrUpdateBoardInfo(BoardInfo info);

        List<LocalIssue> GetIssuesByBoardId(int boardId);
        List<RowInfo> GetRows(int boardId);
        List<ColumnInfo> GetColumns(int boardId);
        LocalIssue GetIssue(int issueId);
        List<BoardInfo> GetAllBoardsInFile();

        void DeleteRow(int rowId);
        void DeleteColumn(int columnId);
        void DeleteIssue(int issueId);
    }
}
