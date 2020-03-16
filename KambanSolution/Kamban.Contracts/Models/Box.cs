using System.Collections.Generic;

namespace Kamban.Contracts
{
    public class Box
    {
        public List<Board> Boards { get; set; } = new List<Board>();
        public List<Row> Rows { get; set; } = new List<Row>();
        public List<Column> Columns { get; set; } = new List<Column>();
        public List<Card> Cards { get; set; } = new List<Card>();
    }
}