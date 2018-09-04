using System;
using System.ComponentModel.DataAnnotations;
using Kamban.SqliteLocalStorage.Entities;

namespace Kamban.SqliteLocalStorage.Context
{
    public class SqliteIssue
    {
        public int Id { get; set; }
        
        public string Head { get; set; }
        public string Body { get; set; }

        [Required]
        public int? RowId { get; set; }
        public RowInfo Row { get; set; }

        [Required]
        public int? ColumnId { get; set; }
        public ColumnInfo Column { get; set; }

        [Required]
        public int? BoardId { get; set; }
        public BoardInfo Board { get; set; }

        public string Color { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
