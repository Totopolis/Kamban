using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Kamban.Export.Options;
using Kamban.Repository.Models;

namespace Kamban.Export
{
    public interface IExportService
    {
        Task ToJson(Box box, string fileName);
        Task ToKamban(Box box, string fileName);
        Task ToXlsx(Box box, string fileName);

        Task ToPdf(Box box,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options);
    }
}