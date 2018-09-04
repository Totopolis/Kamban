using System;

namespace Kamban.SqliteLocalStorage.Entities
{
    public class LocalIssue
    {
        public int Id { get; set; }
        public string Head { get; set; }
        public string Body { get; set; }
        public RowInfo Row { get; set; }
        public ColumnInfo Column { get; set; }
        public BoardInfo Board { get; set; }
        public string Color { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
