using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kamban.Templates
{
    public interface ITemplates
    {
        Task<List<BoardTemplate>> GetBoardTemplates();
    }
}