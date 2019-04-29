using System.Threading.Tasks;
using LiteDB;

namespace Kamban.Repository.LiteDb
{
    public class LiteDbRepository : IRepository
    {
        private readonly LiteDatabase db;

        public LiteDbRepository(string uri)
        {
            db = new LiteDatabase(uri);
        }

        public async Task<Box> Load()
        {
            var cards = db.GetAllAsync<Card>();
            var rows = db.GetAllAsync<Row>();
            var columns = db.GetAllAsync<Column>();
            var boards = db.GetAllAsync<Board>();

            return new Box
            {
                Boards = await boards,
                Cards = await cards,
                Columns = await columns,
                Rows = await rows
            };
        }

        public Task<Row> CreateOrUpdateRow(Row row)
        {
            return db.UpsertAsync(row);
        }

        public Task<Column> CreateOrUpdateColumn(Column column)
        {
            return db.UpsertAsync(column);
        }

        public Task<Card> CreateOrUpdateCard(Card card)
        {
            return db.UpsertAsync(card);
        }

        public Task<Board> CreateOrUpdateBoard(Board board)
        {
            return db.UpsertAsync(board);
        }

        public Task DeleteRow(int rowId)
        {
            return Task.Run(() =>
            {
                var rows = db.GetCollectionByType<Row>();
                rows.Delete(x => x.Id == rowId);
            });
        }

        public Task DeleteColumn(int columnId)
        {
            return Task.Run(() =>
            {
                var columns = db.GetCollectionByType<Column>();
                columns.Delete(x => x.Id == columnId);
            });
        }

        public Task DeleteCard(int cardId)
        {
            return Task.Run(() =>
            {
                var cards = db.GetCollectionByType<Card>();
                cards.Delete(x => x.Id == cardId);
            });
        }

        public Task DeleteBoard(int boardId)
        {
            return Task.Run(() =>
            {
                var boards = db.GetCollectionByType<Board>();
                boards.Delete(x => x.Id == boardId);
            });
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    } //end of class
}