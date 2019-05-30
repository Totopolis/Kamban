using System;
using System.Threading.Tasks;
using Kamban.Repository.Models;

namespace Kamban.Repository
{
    public interface ISaveRepository : IDisposable
    {
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