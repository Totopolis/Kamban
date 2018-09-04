using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ui.Wpf.Common;
using Ui.Wpf.KanbanControl.Dimensions;
using Ui.Wpf.KanbanControl.Dimensions.Generic;
using Ui.Wpf.KanbanControl.Elements.CardElement;
using Autofac;
using Kamban.SqliteLocalStorage;
using Kamban.SqliteLocalStorage.Entities;

namespace Kamban.Models
{
    // TODO: container for boards
    // TODO: local or server access

    public interface IScopeModel
    {
        Task<IDimension> GetColumnHeadersAsync(int boardId);
        Task<IDimension> GetRowHeadersAsync(int boardId);

        Task<List<RowInfo>> GetRowsByBoardIdAsync(int boardId);
        Task<List<ColumnInfo>> GetColumnsByBoardIdAsync(int boardId);
        Task<IEnumerable<LocalIssue>> GetIssuesByBoardIdAsync(int boardId);
        Task<List<BoardInfo>> GetAllBoardsInFileAsync();
        CardContent GetCardContent();
        RowInfo GetSelectedRow(string rowName);
        ColumnInfo GetSelectedColumn(string colName);

        Task DeleteIssueAsync(int issueId);
        Task DeleteRowAsync(int rowId);
        Task DeleteColumnAsync(int columnId);

        Task<BoardInfo> CreateOrUpdateBoardAsync(BoardInfo board);
        Task CreateOrUpdateColumnAsync(ColumnInfo column);
        Task CreateOrUpdateRowAsync(RowInfo row);
        Task CreateOrUpdateIssueAsync(LocalIssue issue);

        Task<LocalIssue> LoadOrCreateIssueAsync(int? issueId);
    }

    public class ScopeModel : IScopeModel
    {
        private readonly SqliteLocalRepository repo;

        private List<RowInfo> rows = new List<RowInfo>();
        private List<ColumnInfo> columns = new List<ColumnInfo>();

        public ScopeModel(IShell shell, string uri)
        {
            repo = shell.Container.Resolve<SqliteLocalRepository>(
                new NamedParameter("conStr", uri));
        }

        #region GettingInfo

        public async Task<List<BoardInfo>> GetAllBoardsInFileAsync()
        {
            return await repo.GetAllBoardsInFileAsync();
        }

        public async Task<IDimension> GetColumnHeadersAsync(int boardId)
        {
            columns.Clear();
            columns = await repo.GetColumnsAsync(boardId);

            var columnHeaders = columns.Select(c => c.Name).ToArray();

            return new TagDimension<string, LocalIssue>(
                tags: columnHeaders,
                getItemTags: i => new[] {i.Column.Name},
                categories: columnHeaders
                    .Select(c => new TagsDimensionCategory<string>(c, c))
                    .Select(tdc => (IDimensionCategory) tdc)
                    .ToArray());
        }

        public async Task<CardsColors> GetTaskColorsAsync(int boardId)
        {
            var isss = await GetIssuesByBoardIdAsync(boardId);

            var cardsColors = new CardsColors
            {
                Path = "pasd",
                ColorMap = isss
                    .ToDictionary(
                        k => (object) k.Id,
                        v => (ICardColor) new CardColor
                        {
                            Background = v.Color,
                            BorderBrush = v.Color
                        })
            };

            return cardsColors;
        }


        public async Task<IDimension> GetRowHeadersAsync(int boardId)
        {
            rows.Clear();
            rows = await repo.GetRowsAsync(boardId);

            var rowHeaders = rows.Select(r => r.Name).ToArray();

            return new TagDimension<string, LocalIssue>(
                tags: rowHeaders,
                getItemTags: i => new[] {i.Row.Name},
                categories: rowHeaders
                    .Select(r => new TagsDimensionCategory<string>(r, r))
                    .Select(tdc => (IDimensionCategory) tdc)
                    .ToArray()
            );
        }

        public async Task<IEnumerable<LocalIssue>> GetIssuesByBoardIdAsync(int boardId)
        {
            return await repo.GetIssuesAsync
            (new NameValueCollection
            {
                {"BoardId", boardId.ToString()}
            });
        }

        public CardContent GetCardContent()
        {
            return new CardContent(new ICardContentItem[]
            {
                new CardContentItem("Head"),
                new CardContentItem("Body", CardContentArea.Additional),
            });
        }

        public RowInfo GetSelectedRow(string rowName)
        {
            return rows.FirstOrDefault(r => r.Name == rowName);
        }

        public ColumnInfo GetSelectedColumn(string colName)
        {
            return columns.FirstOrDefault(c => c.Name == colName);
        }

        public async Task<List<RowInfo>> GetRowsByBoardIdAsync(int boardId)
        {
            return await repo.GetRowsAsync(boardId);
        }

        public async Task<List<ColumnInfo>> GetColumnsByBoardIdAsync(int boardId)
        {
            return await repo.GetColumnsAsync(boardId);
        }

        public async Task<LocalIssue> LoadOrCreateIssueAsync(int? issueId)
        {
            var t = new LocalIssue();
            if (issueId.HasValue)
                t = await repo.GetIssueAsync(issueId.Value);

            return t;
        }

        #endregion

        #region DeletingInfo

        public async Task DeleteIssueAsync(int issueId)
        {
            await repo.DeleteIssueAsync(issueId);
        }

        public async Task DeleteRowAsync(int rowId)
        {
            await repo.DeleteRowAsync(rowId);
        }

        public async Task DeleteColumnAsync(int columnId)
        {
            await repo.DeleteColumnAsync(columnId);
        }

        #endregion

        #region SavingInfo

        public async Task<BoardInfo> CreateOrUpdateBoardAsync(BoardInfo board)
        {
            return await repo.CreateOrUpdateBoardInfoAsync(board);
        }

        public async Task CreateOrUpdateColumnAsync(ColumnInfo column)
        {
            await repo.CreateOrUpdateColumnAsync(column);
        }

        public async Task CreateOrUpdateRowAsync(RowInfo row)
        {
            await repo.CreateOrUpdateRowAsync(row);
        }

        public async Task CreateOrUpdateIssueAsync(LocalIssue issue)
        {
            await repo.CreateOrUpdateIssueAsync(issue);
        }


        #endregion

    }
}
