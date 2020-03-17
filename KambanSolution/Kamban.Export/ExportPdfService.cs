using Kamban.Contracts;
using Kamban.Export.Options;
using PdfSharp.Pdf;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;

namespace Kamban.Export
{
    public class ExportPdfService : IExportService
    {
        public const string EXT_XPS = ".xps";
        public const string EXT_PDF = ".pdf";
        private const int WPF_DPI = 96; // default dpi

        public Task DoExport(Box box, string fileName, object options)
        {
            var opts = options as Tuple<Func<Size, FixedDocument>, PdfOptions>;

            return Task.Run(() =>
            {
                var xpsFileName = fileName + EXT_XPS;

                var pdfPage = new PdfPage
                {
                    Size = opts.Item2.PageSize,
                    Orientation = opts.Item2.PageOrientation
                };

                var width = pdfPage.Width.Inch * WPF_DPI;
                var height = pdfPage.Height.Inch * WPF_DPI;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var document = opts.Item1(new Size(width, height));

                    var xpsd = new XpsDocument(xpsFileName, FileAccess.ReadWrite);
                    var xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
                    xw.Write(document);
                    xpsd.Close();
                });

                PdfSharp.Xps.XpsConverter.Convert(xpsFileName);
                File.Delete(xpsFileName);
            });
        }

        /*        public Task ToPdf(Box box,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options)
        {   
        }*/
    }
}