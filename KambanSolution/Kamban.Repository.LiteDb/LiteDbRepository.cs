using System.Collections.Generic;
using System.Linq;
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

        public Task<List<Card>> GetAllCards()
        {
            return db.GetAllAsync<Card>();
        }

        public Task<List<Row>> GetAllRows()
        {
            return db.GetAllAsync<Row>();
        }

        public Task<List<Column>> GetAllColumns()
        {
            return db.GetAllAsync<Column>();
        }

        public Task<List<Board>> GetAllBoards()
        {
            return db.GetAllAsync<Board>();
        }

        public Task<List<Card>> GetCards(int boardId)
        {
            return Task.Run(() =>
            {
                var cards = db.GetCollectionByType<Card>();
                var results = cards.Find(x => x.BoardId == boardId);

                return results.ToList();
            });
        }

        public Task<List<Row>> GetRows(int boardId)
        {
            return Task.Run(() =>
            {
                var rows = db.GetCollectionByType<Row>();
                var result = rows.Find(x => x.BoardId == boardId);

                return result.ToList();
            });
        }

        public Task<List<Column>> GetColumns(int boardId)
        {
            return Task.Run(() =>
            {
                var columns = db.GetCollectionByType<Column>();
                var result = columns.Find(x => x.BoardId == boardId);

                return result.ToList();
            });
        }

        public Task<Card> GetCard(int cardId)
        {
            return Task.Run(() =>
            {
                var cards = db.GetCollectionByType<Card>();
                var result = cards.Find(x => x.Id == cardId);

                return result.First();
            });
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
    } //end of class
}