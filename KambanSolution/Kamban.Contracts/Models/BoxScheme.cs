using System.Collections.Generic;

namespace Kamban.Contracts
{
    public class BoxScheme
    {
        public List<Board> Boards { get; set; }
        public List<Row> Rows { get; set; }
        public List<Column> Columns { get; set; }
    }
}