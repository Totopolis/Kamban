using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Templates
{
    public class StaticTemplates : ITemplates
    {
        private readonly List<BoardTemplate> _boardTemplates;

        public StaticTemplates()
        {
            _boardTemplates = new List<BoardTemplate>
            {
                new BoardTemplate("Classic", "Classic board",
                    new[] {"Backlog", "ToDo", "InProgress", "Done"},
                    new[] {"Important", "So-so"}),

                new BoardTemplate("Ideas", "Log yours ideas",
                    new[] {"Hardware", "Software", "Business", "Misc"},
                    new[] {"ToDo", "Real", "So-so"})
            };
        }

        public Task<List<BoardTemplate>> GetBoardTemplates()
        {
            return Task.FromResult(_boardTemplates);
        }
    }
}