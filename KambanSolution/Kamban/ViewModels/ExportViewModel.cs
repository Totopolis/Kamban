using DynamicData;
using Kamban.Core;
using Kamban.Model;
using MahApps.Metro.Controls.Dialogs;
using PdfSharp;
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
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private readonly IExportService export;
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

        // Pdf settings
        public PageSize PdfSelectedPageSize { get; set; }
        public PageSize[] PdfPageSizes { get; set; }
        public PageOrientation PdfSelectedPageOrientation { get; set; }
        public PageOrientation[] PdfPageOrientations { get; set; }
        public bool PdfStretch { get; set; }

        [Reactive] public string TargetFolder { get; set; }
        [Reactive] public string TargetFile { get; set; }

        [Reactive] public bool DatePostfix { get; set; }
        [Reactive] public bool SplitBoardsToFiles { get; set; }

        public ReactiveCommand<Unit, Unit> SelectTargetFolderCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ExportViewModel(IAppModel am, IDialogCoordinator dc, IAppConfig cfg, IExportService ex)
        {
            appModel = am;
            dialCoord = dc;
            export = ex;

            AvailableDbs = appModel.Dbs;

            boards = new SourceList<BoardToExport>();
            AvailableBoards = boards.SpawnCollection();

            ExportJson = true;
            DatePostfix = true;
            SplitBoardsToFiles = false;

            PdfPageSizes = Enum.GetValues(typeof(PageSize)).Cast<PageSize>().ToArray();
            PdfSelectedPageSize = PageSize.A4;
            PdfPageOrientations = Enum.GetValues(typeof(PageOrientation)).Cast<PageOrientation>().ToArray();
            PdfSelectedPageOrientation = PageOrientation.Portrait;
            PdfStretch = true;

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
            var boardsSelected = await GetBoardsSelectedToExport();

            // 1. prepare database
            var jb = new DatabaseToExport();
            jb.BoardList.AddRange(boardsSelected);

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
            var boardsSelected = await GetBoardsSelectedToExport();

            foreach (var brd in boardsSelected)
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

        private async Task<IEnumerable<BoardInfo>> GetBoardsSelectedToExport()
        {
            var selectedBoardIds = new HashSet<int>(
                AvailableBoards
                    .Where(x => x.IsChecked)
                    .Select(x => x.Board.Id)
            );

            var boardsAll = await prjService.GetAllBoardsInFileAsync();
            return boardsAll.Where(x => selectedBoardIds.Contains(x.Id));
        }

        private void DoExportForNeededFormats(DatabaseToExport db, string fileName)
        {
            if (ExportJson)
                export.ToJson(db, fileName);

            if (ExportKamban)
                export.ToKamban(db, fileName);

            if (ExportXlsx)
                export.ToXlsx(db, fileName);

            if (ExportPdf)
                export.ToPdf(db, SelectedDb, fileName, 
                    PdfSelectedPageSize, PdfSelectedPageOrientation, PdfStretch);
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
