using System;
using System.Threading.Tasks;

namespace Kamban.Contracts
{
    public interface IExportService
    {
        Task DoExport(Box box, string fileName, object options);
    }
}
