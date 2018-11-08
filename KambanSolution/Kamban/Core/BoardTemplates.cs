using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;

namespace Kamban.Core
{
    public class BoardTemplate : ReactiveObject
    {
        [Reactive] public string Name { get; set; }
        [Reactive] public string Description { get; set; }
        [Reactive] public string Authtor { get; set; }

        [Reactive] public List<string> Categories { get; set; }

        [Reactive] public List<ColumnViewModel> Columns { get; set; }
        [Reactive] public List<RowViewModel> Rows { get; set; }
        [Reactive] public List<CardViewModel> Cards { get; set; }

        // [Reactive] properties[]
    }

    public static class InternalBoardTemplates
    {
        public static List<BoardTemplate> Templates { get; set; } =
            new List<BoardTemplate>();

        static InternalBoardTemplates()
        {
            AddBoard("Classic", "Classic board",
                new string[] { "Backlog", "ToDo", "InProgress", "Done" },
                new string[] { "Important", "So-so"});

            AddBoard("Ideas", "Log yours ideas",
                new string[] { "Hardware", "Software", "Business", "Misc" },
                new string[] { "ToDo", "Real", "So-so" });
        }

        private static void AddBoard(string name, string descr, string[] columns, string[] rows)
        {
            var t1 = new BoardTemplate
            {
                Name = name,
                Description = descr,
                Authtor = "TopolSystems",
                //Categories = { "12", "23" },
                Columns = new List<ColumnViewModel>() { },
                Rows = new List<RowViewModel>()
            };

            for (int i = 0; i < columns.Length; i++)
                t1.AddColumn(columns[i], i);

            for (int i = 0; i < rows.Length; i++)
                t1.AddRow(rows[i], i);

            Templates.Add(t1);
        }

        private static void AddColumn(this BoardTemplate tmp, string name, int order)
        {
            tmp.Columns.Add(new ColumnViewModel
            {
                Name = name,
                Order = order
            });
        }

        private static void AddRow(this BoardTemplate tmp, string name, int order)
        {
            tmp.Rows.Add(new RowViewModel
            {
                Name = name,
                Order = order
            });
        }
    }//end of class
}
