using System.Collections.Generic;

namespace Kamban.Repository.Models
{
    public class BoxScheme
    {
        public List<Board> Boards { get; set; }
        public List<Row> Rows { get; set; }
        public List<Column> Columns { get; set; }
    }
}