using System;
using System.Windows;
using System.Windows.Documents;
using Kamban.Export.Options;

namespace Kamban.Export
{
    public interface IExportService
    {
        void ToJson(BoxToExport box, string fileName);
        void ToKamban(BoxToExport box, string fileName);
        void ToXlsx(BoxToExport box, string fileName);

        void ToPdf(BoxToExport box,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options);
    }
}