using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Kamban.Export.Options;

namespace Kamban.Export
{
    public interface IExportService
    {
        Task ToJson(BoxToExport box, string fileName);
        Task ToKamban(BoxToExport box, string fileName);
        Task ToXlsx(BoxToExport box, string fileName);

        Task ToPdf(BoxToExport box,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options);
    }
}