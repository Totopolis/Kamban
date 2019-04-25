using Kamban.Common;
using Kamban.Export;
using Kamban.Export.Options;
using Kamban.Repository.LiteDb;
using Newtonsoft.Json;
using OfficeOpenXml;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;

namespace Kamban.Core
{
    public interface IExportService
    {
        void ToJson(BoxToExport db, string fileName);
        void ToKamban(BoxToExport db, string fileName);
        void ToXlsx(BoxToExport db, string fileName);

        void ToPdf(BoxToExport db,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options);
    }

    public class ExportService : IExportService
    {
        public const string EXT_XPS = ".xps";
        public const string EXT_PDF = ".pdf";
        public const string EXT_KAM = ".kam";
        public const string EXT_XLSX = ".xlsx";
        public const string EXT_JSON = ".json";

        private const int WPF_DPI = 96; // default dpi


        public void ToJson(BoxToExport db, string fileName)
        {
            var jsonFileName = fileName + EXT_JSON;

            var output = JsonConvert.SerializeObject(db, Formatting.Indented);
            File.WriteAllText(jsonFileName, output);
        }

        public void ToKamban(BoxToExport db, string fileName)
        {
            var kamFileName = fileName + EXT_KAM;

            var repo = new LiteDbRepository(kamFileName);

            foreach (var brd in db.BoardList)
            {
                repo.CreateOrUpdateBoardInfo(brd);

                foreach (var col in db.ColumnList)
                    repo.CreateOrUpdateColumn(col);

                foreach (var row in db.RowList)
                    repo.CreateOrUpdateRow(row);

                foreach (var iss in db.IssueList)
                    repo.CreateOrUpdateIssue(iss);
            }
        }

        public void ToXlsx(BoxToExport db, string fileName)
        {
            var xlsxFileName = fileName + EXT_XLSX;

            using (var package = new ExcelPackage())
            {
                var boardsWithIssues =
                    from b in db.BoardList
                    join g in
                        from i in db.IssueList group i by i.BoardId
                        on b.Id equals g.Key into bg
                    from g in bg.DefaultIfEmpty()
                    select new {Info = b, Issues = g?.ToList()};

                foreach (var board in boardsWithIssues)
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

                    if (board.Issues == null)
                        continue;

                    var issues =
                        from i in board.Issues
                        join r in db.RowList on i.RowId equals r.Id
                        join c in db.ColumnList on i.ColumnId equals c.Id
                        orderby c.Id, r.Id, i.Order, i.Id
                        select new {Info = i, RowInfo = r, ColInfo = c};

                    var row = 2;
                    foreach (var issue in issues)
                    {
                        var values = new object[]
                        {
                            issue.Info.Id,
                            issue.Info.Head,
                            issue.RowInfo.Name,
                            issue.ColInfo.Name,
                            ColorItem.ToColorName(issue.Info.Color),
                            issue.Info.Body,
                            issue.Info.Created,
                            issue.Info.Modified
                        };

                        WriteValuesToSheet(sheet, row, values);
                        ++row;
                    }

                    sheet.Cells.AutoFitColumns();
                }

                var xlFile = new FileInfo(xlsxFileName);
                package.SaveAs(xlFile);
            }
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

        public void ToPdf(BoxToExport db,
            Func<Size, FixedDocument> renderToXps,
            string fileName, PdfOptions options)
        {
            var pdfPage = new PdfPage
            {
                Size = options.PageSize,
                Orientation = options.PageOrientation
            };

            var width = pdfPage.Width.Inch * WPF_DPI;
            var height = pdfPage.Height.Inch * WPF_DPI;

            var document = renderToXps(new Size(width, height));

            var xpsFileName = fileName + EXT_XPS;

            var xpsd = new XpsDocument(xpsFileName, FileAccess.ReadWrite);
            var xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
            xw.Write(document);
            xpsd.Close();

            PdfSharp.Xps.XpsConverter.Convert(xpsFileName);
            File.Delete(xpsFileName);
        }
    }
}