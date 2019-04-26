using System.Collections.Generic;
using Kamban.Repository;

namespace Kamban.Export
{
    public class BoxToExport
    {
        public List<Board> BoardList { get; set; } = new List<Board>();
        public List<Column> ColumnList { get; set; } = new List<Column>();
        public List<Row> RowList { get; set; } = new List<Row>();
        public List<Card> CardList { get; set; } = new List<Card>();
    }
}