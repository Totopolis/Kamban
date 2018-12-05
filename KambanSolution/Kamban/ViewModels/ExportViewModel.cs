using DynamicData;
using Kamban.Core;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using OfficeOpenXml;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class BoardToExport : ReactiveObject
    {
        [Reactive] public BoardViewModel Board { get; set; }
        [Reactive] public bool IsChecked { get; set; }
    }

    public class DatabaseToExport
    {
        public List<BoardInfo> BoardList { get; set; } = new List<BoardInfo>();
        public List<ColumnInfo> ColumnList { get; set; } = new List<ColumnInfo>();
        public List<RowInfo> RowList { get; set; } = new List<RowInfo>();
        public List<Issue> IssueList { get; set; } = new List<Issue>();
    }

    public class ExportViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private IProjectService prjService;
        private SourceList<BoardToExport> boards;

        [Reactive] public ReadOnlyObservableCollection<DbViewModel> AvailableDbs { get; set; }
        [Reactive] public DbViewModel SelectedDb { get; set; }
        [Reactive] public ReadOnlyObservableCollection<BoardToExport> AvailableBoards { get; set; }

        [Reactive] public bool ExportJson { get; set; }
        [Reactive] public bool ExportXml { get; set; }
        [Reactive] public bool ExportKamban { get; set; }
        [Reactive] public bool ExportXlsx { get; set; }
        [Reactive] public bool ExportPdf { get; set; }

        [Reactive] public string TargetFolder { get; set; }
        [Reactive] public string TargetFile { get; set; }

        [Reactive] public bool DatePostfix { get; set; }
        [Reactive] public bool SplitBoardsToFiles { get; set; }

        public ReactiveCommand<Unit, Unit> SelectTargetFolderCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ExportViewModel(IShell shell, IAppModel am, IDialogCoordinator dc, IAppConfig cfg)
        {
            this.shell = shell as IShell;
            this.appModel = am;
            dialCoord = dc;

            AvailableDbs = appModel.Dbs;

            boards = new SourceList<BoardToExport>();
            AvailableBoards = boards.SpawnCollection();

            ExportJson = true;
            DatePostfix = true;
            SplitBoardsToFiles = false;

            var canExport = boards
                .Connect()
                .AutoRefresh()
                .Filter(x=>x.IsChecked)
                .Select(x => AvailableBoards.Count(y => y.IsChecked) > 0
                    && !string.IsNullOrEmpty(SelectedDb.Uri) && File.Exists(SelectedDb.Uri));

            ExportCommand = ReactiveCommand.CreateFromTask(ExportCommandExecute, canExport);
            SelectTargetFolderCommand = ReactiveCommand.Create(SelectTargetFolderCommandExecute);
            CancelCommand = ReactiveCommand.Create(() => this.Close());

            this.ObservableForProperty(x => x.SelectedDb)
                .Where(x => x.Value != null)
                .Select(x => x.Value)
                .Subscribe(db =>
                {
                    boards.ClearAndAddRange(db.Boards.Items
                        .Select(x => new BoardToExport { Board = x, IsChecked = true }));
                    
                    TargetFile = Path.GetFileNameWithoutExtension(db.Uri) + "_export";
                });

            this.ObservableForProperty(x => x.TargetFolder)
                .Subscribe(x => cfg.ArchiveFolder = x.Value);

            SelectedDb = AvailableDbs.FirstOrDefault();

            var fi = new FileInfo(SelectedDb.Uri);
            TargetFolder = cfg.ArchiveFolder ?? fi.DirectoryName;
        }

        private async Task ExportCommandExecute()
        {
            if (!Directory.Exists(TargetFolder))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Target directory not found");
                return;
            }

            prjService = appModel.GetProjectService(SelectedDb.Uri);

            string fileName = TargetFolder + "\\" + TargetFile;
            if (DatePostfix)
                fileName += "_" + DateTime.Now.ToString("yyyyMMdd");

            if (!SplitBoardsToFiles)
                await DoExportWhole(fileName);
            else
                await DoExportSplit(fileName);

            await dialCoord.ShowMessageAsync(this, "Info", "Process finished");
        }

        private async Task DoExportWhole(string fileName)
        {
            // 1. prepare database
            var jb = new DatabaseToExport();
            jb.BoardList.AddRange(await prjService.GetAllBoardsInFileAsync());

            foreach (var brd in jb.BoardList)
            {
                var columns = await prjService.GetColumnsByBoardIdAsync(brd.Id);
                jb.ColumnList.AddRange(columns);

                var rows = await prjService.GetRowsByBoardIdAsync(brd.Id);
                jb.RowList.AddRange(rows);

                var issues = await prjService.GetIssuesByBoardIdAsync(brd.Id);
                jb.IssueList.AddRange(issues);
            }

            // 2. export
            DoExportForNeededFormats(jb, fileName);
        }

        private async Task DoExportSplit(string fileName)
        {
            var boards = await prjService.GetAllBoardsInFileAsync();

            foreach (var brd in boards)
            {
                // 1. prepare database
                var jb = new DatabaseToExport();
                jb.BoardList.Add(brd);

                var columns = await prjService.GetColumnsByBoardIdAsync(brd.Id);
                jb.ColumnList.AddRange(columns);
                var rows = await prjService.GetRowsByBoardIdAsync(brd.Id);
                jb.RowList.AddRange(rows);
                var issues = await prjService.GetIssuesByBoardIdAsync(brd.Id);
                jb.IssueList.AddRange(issues);

                // 2. export
                DoExportForNeededFormats(jb, fileName + "_" + brd.Name);
            }
        }

        private void DoExportForNeededFormats(DatabaseToExport db, string fileName)
        {
            if (ExportJson)
                DoExportJson(db, fileName + ".json");

            if (ExportKamban)
                DoExportKamban(db, fileName + ".kam");

            if (ExportXlsx)
                DoExportXlsx(db, fileName + ".xlsx");

            if (ExportPdf)
                DoExportPdf(db, fileName + ".pdf");
        }

        private void DoExportJson(DatabaseToExport db, string fileName)
        {
            if (File.Exists(fileName))
            {
                dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            string output = JsonConvert.SerializeObject(db, Formatting.Indented);
            File.WriteAllText(fileName, output);
        }

        private void DoExportKamban(DatabaseToExport db, string fileName)
        {
            if (File.Exists(fileName))
            {
                dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            var prj = appModel.GetProjectService(fileName);

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

        private void DoExportXlsx(DatabaseToExport db, string fileName)
        {
            if (File.Exists(fileName))
            {
                dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
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

                var xlFile = new FileInfo(fileName);
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

        private void DoExportPdf(DatabaseToExport db, string fileName)
        {
            if (File.Exists(fileName))
            {
                dialCoord.ShowMessageAsync(this, "Error", "Target file already exists");
                return;
            }

            var pdf = new PdfDocument();

            foreach (var board in SelectedDb.Boards.Items)
            {
                var pdfPage = pdf.AddPage();
                pdfPage.Size = PageSize.A4;
                pdfPage.Orientation = PageOrientation.Landscape;

                var bmpSource = ((ShellEx) shell).RenderView<BoardView>(
                    new BoardViewRequest
                    {
                        ViewId = SelectedDb.Uri,
                        Db = SelectedDb,
                        Board = board
                    },
                    72, pdfPage.Width.Inch, pdfPage.Height.Inch);

                var png = new PngBitmapEncoder { Frames = { BitmapFrame.Create(bmpSource) } };
                using (var stream = new MemoryStream())
                {
                    png.Save(stream);
                    var xImage = XImage.FromStream(stream);
                    var gfx = XGraphics.FromPdfPage(pdfPage);
                    gfx.DrawImage(xImage, 0, 0, xImage.PixelWidth, xImage.PixelHeight);
                }
            }

            pdf.Save(fileName);
        }

        private void SelectTargetFolderCommandExecute()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                SelectedPath = TargetFolder
            };
            //dialog.RootFolder = Environment.SpecialFolder.MyDocuments;

            if (dialog.ShowDialog() == DialogResult.OK)
                TargetFolder = dialog.SelectedPath;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            Title = "Export";
        }
    }
}
