using Kamban.Model;
using Kamban.ViewModels;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using OfficeOpenXml;
using PdfSharp;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Xps.Packaging;
using Ui.Wpf.Common;

namespace Kamban.Core
{
    public interface IExportService
    {
        void ToJson(DatabaseToExport db, string fileName);
        void ToKamban(DatabaseToExport db, string fileName);
        void ToXlsx(DatabaseToExport db, string fileName);
        void ToPdf(DatabaseToExport db, DbViewModel dbViewModel, string fileName, PdfOptions options);
    }

    public class ExportService : IExportService
    {
        public const string EXT_XPS = ".xps";
        public const string EXT_PDF = ".pdf";
        public const string EXT_KAM = ".kam";
        public const string EXT_XLSX = ".xlsx";
        public const string EXT_JSON = ".json";

        private const int WPF_DPI = 96; // default dpi

        private readonly IShell _shell;
        private readonly IDialogCoordinator _dialCoord;
        private readonly IAppModel _appModel;


        public ExportService(IShell shell, IDialogCoordinator dialCoord, IAppModel appModel)
        {
            _shell = shell;
            _dialCoord = dialCoord;
            _appModel = appModel;
        }


        public void ToJson(DatabaseToExport db, string fileName)
        {
            var jsonFileName = fileName + EXT_JSON;

            if (File.Exists(jsonFileName))
            {
                _dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            var output = JsonConvert.SerializeObject(db, Formatting.Indented);
            File.WriteAllText(jsonFileName, output);
        }

        public void ToKamban(DatabaseToExport db, string fileName)
        {
            var kamFileName = fileName + EXT_KAM;

            if (File.Exists(kamFileName))
            {
                _dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            var prj = _appModel.GetProjectService(kamFileName);

            foreach (var brd in db.BoardList)
            {
                prj.CreateOrUpdateBoardAsync(brd);

                foreach (var col in db.ColumnList)
                    prj.CreateOrUpdateColumnAsync(col);

                foreach (var row in db.RowList)
                    prj.CreateOrUpdateRowAsync(row);

                foreach (var iss in db.IssueList)
                    prj.CreateOrUpdateIssueAsync(iss);
            }
        }

        public void ToXlsx(DatabaseToExport db, string fileName)
        {
            var xlsxFileName = fileName + EXT_XLSX;

            if (File.Exists(xlsxFileName))
            {
                _dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            using (var package = new ExcelPackage())
            {
                var boardsWithIssues =
                    from b in db.BoardList
                    join g in
                        from i in db.IssueList group i by i.BoardId
                        on b.Id equals g.Key into bg
                    from g in bg.DefaultIfEmpty()
                    select new { Info = b, Issues = g?.ToList() };

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
                        select new { Info = i, RowInfo = r, ColInfo = c };

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

        public void ToPdf(DatabaseToExport db, DbViewModel dbViewModel, string fileName, PdfOptions options)
        {
            var pdfFileName = fileName + EXT_PDF;

            if (File.Exists(pdfFileName))
            {
                _dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            var pdfPage = new PdfPage
            {
                Size = options.PageSize,
                Orientation = options.PageOrientation
            };

            var width = pdfPage.Width.Inch * WPF_DPI;
            var height = pdfPage.Height.Inch * WPF_DPI;

            var selectedBoardIds = new HashSet<int>(db.BoardList.Select(x => x.Id));
            var document = ((ShellEx)_shell).ViewsToDocument<BoardForExportView>(
                dbViewModel.Boards.Items
                    .Where(x => selectedBoardIds.Contains(x.Id))
                    .Select(x =>
                        new BoardViewRequest
                        {
                            ViewId = dbViewModel.Uri,
                            Db = dbViewModel,
                            Board = x
                        })
                    .Cast<ViewRequest>()
                    .ToArray(),
                new Size(width, height),
                options.ScaleOptions);

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
