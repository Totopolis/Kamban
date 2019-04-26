using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Repository
{
    public interface IRepository
    {
        Task<Card> CreateOrUpdateCard(Card card);
        Task<Row> CreateOrUpdateRow(Row row);
        Task<Column> CreateOrUpdateColumn(Column column);        
        Task<Board> CreateOrUpdateBoard(Board board);

        Task<List<Card>> GetAllCards();
        Task<List<Row>> GetAllRows();
        Task<List<Column>> GetAllColumns();
        Task<List<Board>> GetAllBoards();

        Task<List<Card>> GetCards(int boardId);
        Task<List<Row>> GetRows(int boardId);
        Task<List<Column>> GetColumns(int boardId);

        Task<Card> GetCard(int cardId);

        Task DeleteRow(int rowId);
        Task DeleteColumn(int columnId);
        Task DeleteCard(int cardId);
        Task DeleteBoard(int boardId);
    }
}
