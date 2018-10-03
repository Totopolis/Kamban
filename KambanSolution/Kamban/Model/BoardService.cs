using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ui.Wpf.Common;
using Ui.Wpf.KanbanControl.Dimensions;
using Ui.Wpf.KanbanControl.Dimensions.Generic;
using Ui.Wpf.KanbanControl.Elements.CardElement;
using Kamban.Repository;

namespace Kamban.Model
{
    /// LiteDb or Api access to board by url
    public interface IBoardService
    {
        string Uri { get; set; }

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

        // Obsolete

        Task<IDimension> GetColumnHeadersAsync(int boardId);
        Task<IDimension> GetRowHeadersAsync(int boardId);
        
        CardContent GetCardContent();
        RowInfo GetSelectedRow(string rowName);
        ColumnInfo GetSelectedColumn(string colName);

        Task<Issue> LoadOrCreateIssueAsync(int? issueId);
    }

    public class BoardService : IBoardService
    {
        private readonly IRepository repo;

        private List<RowInfo> rows = new List<RowInfo>();
        private List<ColumnInfo> columns = new List<ColumnInfo>();

        public BoardService(IShell shell, IRepository repository, string uri)
        {
            Uri = uri;
            repo = repository;
            repo.Initialize(uri);
        }

        public string Uri { get; set; }

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
            return await Task.Run(() => repo.GetColumns(boardId));
        }

        public async Task<List<RowInfo>> GetRowsByBoardIdAsync(int boardId)
        {
            return await Task.Run(() => repo.GetRows(boardId));
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

        #region Obsolete

        public async Task<IDimension> GetColumnHeadersAsync(int boardId)
        {
            columns.Clear();
            columns = await Task.Run(() => repo.GetColumns(boardId));

            var columnHeaders = columns.Select(c => c.Name).ToArray();

            return new TagDimension<string, Issue>(
                tags: columnHeaders,
                getItemTags: i => new[] {
                    columns.Where(x => x.Id == i.ColumnId).Select(x => x.Name).FirstOrDefault()
                },
                categories: columnHeaders
                    .Select(c => new TagsDimensionCategory<string>(c, c))
                    .Select(tdc => (IDimensionCategory)tdc)
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
            rows = await Task.Run(() => repo.GetRows(boardId));

            var rowHeaders = rows.Select(r => r.Name).ToArray();

            return new TagDimension<string, Issue>(
                tags: rowHeaders,
                getItemTags: i => new[] {
                    rows.Where(x=>x.Id == i.RowId).Select(x=>x.Name).FirstOrDefault()
                }, //i.Row.Name},
                categories: rowHeaders
                    .Select(r => new TagsDimensionCategory<string>(r, r))
                    .Select(tdc => (IDimensionCategory)tdc)
                    .ToArray()
            );
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
