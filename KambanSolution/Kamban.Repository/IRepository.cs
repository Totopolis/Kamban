using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public interface IRepository : IDisposable
    {
        Task<Box> Load();

        Task<Card> CreateOrUpdateCard(Card card);
        Task<Row> CreateOrUpdateRow(Row row);
        Task<Column> CreateOrUpdateColumn(Column column);
        Task<Board> CreateOrUpdateBoard(Board board);

        Task DeleteRow(int rowId);
        Task DeleteColumn(int columnId);
        Task DeleteCard(int cardId);
        Task DeleteBoard(int boardId);
    }
}