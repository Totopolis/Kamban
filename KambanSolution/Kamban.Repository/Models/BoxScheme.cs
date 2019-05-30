using System.Collections.Generic;

namespace Kamban.Repository.Models
{
    public class BoxScheme
    {
        public List<Board> Boards { get; set; } = new List<Board>();
        public List<Row> Rows { get; set; } = new List<Row>();
        public List<Column> Columns { get; set; } = new List<Column>();
    }
}