using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kamban.Contracts;
using LiteDB;

namespace Kamban.Repository.LiteDb
{
    public class LiteDbRepository : IRepository
    {
        private readonly LiteDbManager manager;

        public LiteDbRepository(string uri)
        {
            manager = new LiteDbManager(uri);
        }

        public void Dispose()
        {
            manager.CloseDb();
        }

        public async Task<Box> Load()
        {
            var db = manager.LockDb();

            try
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
            finally
            {
                manager.FreeDb();
            }
        }

        public async Task<BoxScheme> LoadScheme()
        {
            var db = manager.LockDb();

            try
            {
                var rows = db.GetAllAsync<Row>();
                var columns = db.GetAllAsync<Column>();
                var boards = db.GetAllAsync<Board>();

                return new BoxScheme
                {
                    Boards = await boards,
                    Columns = await columns,
                    Rows = await rows
                };
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task<List<Board>> LoadSchemeBoards()
        {
            var db = manager.LockDb();

            try
            {
                return db.GetAllAsync<Board>();
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public async Task<List<Column>> LoadSchemeColumns(int[] boardIds = null)
        {
            var db = manager.LockDb();

            try
            {
                var columns = await db.GetAllAsync<Column>();
                return columns
                    .Where(x => boardIds?.Contains(x.BoardId) ?? true)
                    .ToList();
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public async Task<List<Row>> LoadSchemeRows(int[] boardIds = null)
        {
            var db = manager.LockDb();

            try
            {
                var rows = await db.GetAllAsync<Row>();
                return rows
                    .Where(x => boardIds?.Contains(x.BoardId) ?? true)
                    .ToList();
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public async Task<List<Card>> LoadCards(CardFilter filter)
        {
            var db = manager.LockDb();

            try
            {
                var cards = await db.GetAllAsync<Card>();
                if (filter.IsEmpty)
                    return cards;

                var filteredCards =
                    from card in cards
                    join boardId in filter.BoardIds on card.BoardId equals boardId
                    join columnId in filter.ColumnIds on card.ColumnId equals columnId
                    join rowId in filter.RowIds on card.ColumnId equals rowId
                    select card;

                return filteredCards.ToList();
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task<Row> CreateOrUpdateRow(Row row)
        {
            var db = manager.LockDb();

            try
            {
                return db.UpsertAsync(row);
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task<Column> CreateOrUpdateColumn(Column column)
        {
            var db = manager.LockDb();

            try
            {
                return db.UpsertAsync(column);
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task<Card> CreateOrUpdateCard(Card card)
        {
            var db = manager.LockDb();

            try
            {
                return db.UpsertAsync(card);
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task<Board> CreateOrUpdateBoard(Board board)
        {
            var db = manager.LockDb();

            try
            {
                return db.UpsertAsync(board);
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task DeleteRow(int rowId)
        {
            var db = manager.LockDb();

            try
            {
                return Task.Run(() =>
                {
                    var rows = db.GetCollectionByType<Row>();
                    rows.DeleteMany(x => x.Id == rowId);
                });
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task DeleteColumn(int columnId)
        {
            var db = manager.LockDb();

            try
            {
                return Task.Run(() =>
                {
                    var columns = db.GetCollectionByType<Column>();
                    columns.DeleteMany(x => x.Id == columnId);
                });
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task DeleteCard(int cardId)
        {
            var db = manager.LockDb();

            try
            {
                return Task.Run(() =>
                {
                    var cards = db.GetCollectionByType<Card>();
                    cards.DeleteMany(x => x.Id == cardId);
                });
            }
            finally
            {
                manager.FreeDb();
            }
        }

        public Task DeleteBoard(int boardId)
        {
            var db = manager.LockDb();

            try
            {
                return Task.Run(() =>
                {
                    var boards = db.GetCollectionByType<Board>();
                    boards.DeleteMany(x => x.Id == boardId);
                });
            }
            finally
            {
                manager.FreeDb();
            }
        }

    } //end of class
}
