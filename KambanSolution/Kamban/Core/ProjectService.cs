using Kamban.Repository;
using Kamban.Repository.LiteDb;

namespace Kamban.Core
{
    /// LiteDb or Api access to board by url
    public interface IProjectService
    {
        IRepository Repository { get; set; }
    }

    public class ProjectService : IProjectService
    {
        public ProjectService(string uri)
        {
            // ToDo: check if uri is local file or url

            Repository = new LiteDbRepository(uri);
        }

        public IRepository Repository { get; set; }
    } //end of class
}