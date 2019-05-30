using System.Collections.Generic;
using Kamban.Repository.Models;

namespace Kamban.Templates
{
    public class BoardTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }

        public List<Column> Columns { get; set; } = new List<Column>();
        public List<Row> Rows { get; set; } = new List<Row>();
        public List<Card> Cards { get; set; } = new List<Card>();

        public BoardTemplate(string name, string description, string[] columns, string[] rows)
        {
            Name = name;
            Description = description;

            for (var i = 0; i < columns.Length; i++)
                Columns.Add(new Column
                {
                    Name = columns[i],
                    Order = i
                });

            for (var i = 0; i < rows.Length; i++)
                Rows.Add(new Row
                {
                    Name = rows[i],
                    Order = i
                });
        }
    }
}