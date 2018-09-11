using System;

namespace Kamban.SqliteLocalStorage.Entities
{
    public class LocalIssue
    {
        public int Id { get; set; }
        public string Head { get; set; }
        public string Body { get; set; }
        public int RowId { get; set; }
        public int ColumnId { get; set; }
        public int BoardId { get; set; }
        public string Color { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
