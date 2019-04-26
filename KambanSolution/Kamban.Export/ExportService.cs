using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using Kamban.Common;
using Kamban.Export.Options;
using Kamban.Repository.LiteDb;
using Newtonsoft.Json;
using OfficeOpenXml;
using PdfSharp.Pdf;

namespace Kamban.Export
{
    public class ExportService : IExportService
    {
        public const string EXT_XPS = ".xps";
        public const string EXT_PDF = ".pdf";
        public const string EXT_KAM = ".kam";
        public const string EXT_XLSX = ".xlsx";
        public const string EXT_JSON = ".json";

        private const int WPF_DPI = 96; // default dpi


        public Task ToJson(BoxToExport box, string fileName)
        {
            return Task.Run(() =>
            {
                var jsonFileName = fileName + EXT_JSON;

                var output = JsonConvert.SerializeObject(box, Formatting.Indented);
                File.WriteAllText(jsonFileName, output);
            });
        }

        public async Task ToKamban(BoxToExport box, string fileName)
        {
            var kamFileName = fileName + EXT_KAM;

            using (var repo = new LiteDbRepository(kamFileName))
            {
                foreach (var brd in box.BoardList)
                {
                    await repo.CreateOrUpdateBoard(brd);

                    foreach (var col in box.ColumnList)
                        await repo.CreateOrUpdateColumn(col);

                    foreach (var row in box.RowList)
                        await repo.CreateOrUpdateRow(row);

                    foreach (var iss in box.CardList)
                        await repo.CreateOrUpdateCard(iss);
                }
            }
        }

        public Task ToXlsx(BoxToExport box, string fileName)
        {
            return Task.Run(() =>
            {
                var xlsxFileName = fileName + EXT_XLSX;

                using (var package = new ExcelPackage())
                {
                    var boardsWithCards =
                        from b in box.BoardList
                        join g in
                            from i in box.CardList group i by i.BoardId
                            on b.Id equals g.Key into bg
                        from g in bg.DefaultIfEmpty()
                        select new {Info = b, Cards = g?.ToList()};

                    foreach (var board in boardsWithCards)
                    {
                        var sheet = package.Workbook.Worksheets.Add(board.Info.Name);

                        WriteValuesToSheet(sheet, 1,
                            new[]
                            {
                                "Id",
                                "Name",
                                "Row",
                                "Column",
                                "Color",
                                "Description",
                                "Crated",
                                "Modified"
                            });

                        if (board.Cards == null)
                            continue;

                        var cards =
                            from i in board.Cards
                            join r in box.RowList on i.RowId equals r.Id
                            join c in box.ColumnList on i.ColumnId equals c.Id
                            orderby c.Id, r.Id, i.Order, i.Id
                            select new {Info = i, RowInfo = r, ColInfo = c};

                        var row = 2;
                        foreach (var card in cards)
                        {
                            var values = new object[]
                            {
                                card.Info.Id,
                                card.Info.Head,
                                card.RowInfo.Name,
                                card.ColInfo.Name,
                                ColorItem.ToColorName(card.Info.Color),
                                card.Info.Body,
                                card.Info.Created,
                                card.Info.Modified
                            };

                            WriteValuesToSheet(sheet, row, values);
                            ++row;
                        }

                        sheet.Cells.AutoFitColumns();
                    }

                    var xlFile = new FileInfo(xlsxFileName);
                    package.SaveAs(xlFile);
                }
            });
        }

        private static void WriteValuesToSheet(ExcelWorksheet sheet, int row, IEnumerable<object> values)
        {
            var col = 1;
            foreach (var val in values)
            {
                sheet.Cells[row, col].Value = val;
                if (val is DateTime)
                    sheet.Cells[row, col].Style.Numberformat.Format = "hh:mm:ss dd.mm.yyyy";
                ++col;
            }
        }

        public Task ToPdf(BoxToExport box,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options)
        {
            return Task.Run(() =>
            {
                var xpsFileName = fileName + EXT_XPS;

                var pdfPage = new PdfPage
                {
                    Size = options.PageSize,
                    Orientation = options.PageOrientation
                };

                var width = pdfPage.Width.Inch * WPF_DPI;
                var height = pdfPage.Height.Inch * WPF_DPI;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var document = renderToXps(new Size(width, height));

                    var xpsd = new XpsDocument(xpsFileName, FileAccess.ReadWrite);
                    var xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
                    xw.Write(document);
                    xpsd.Close();
                });

                PdfSharp.Xps.XpsConverter.Convert(xpsFileName);
                File.Delete(xpsFileName);
            });
        }
    }
}