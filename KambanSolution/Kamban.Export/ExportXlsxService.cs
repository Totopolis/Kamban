using Kamban.Common;
using Kamban.Contracts;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kamban.Export
{
    public class ExportXlsxService : IExportService
    {
        public const string EXT_XLSX = ".xlsx";

        public Task DoExport(Box box, string fileName, object options)
        {
            return Task.Run(() =>
            {
                var xlsxFileName = fileName + EXT_XLSX;

                using (var package = new ExcelPackage())
                {
                    var boardsWithCards =
                        from b in box.Boards
                        join g in
                            from i in box.Cards group i by i.BoardId
                            on b.Id equals g.Key into bg
                        from g in bg.DefaultIfEmpty()
                        select new { Info = b, Cards = g?.ToList() };

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
                            join r in box.Rows on i.RowId equals r.Id
                            join c in box.Columns on i.ColumnId equals c.Id
                            orderby c.Id, r.Id, i.Order, i.Id
                            select new { Info = i, RowInfo = r, ColInfo = c };

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
    }
}
