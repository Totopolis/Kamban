using Kamban.Model;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class BoardToExport : ReactiveObject
    {
        [Reactive] public BoardInfo Board { get; set; }
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
        private IProjectService prjService;

        [Reactive] public ReactiveList<string> AvailableFiles { get; set; }
        [Reactive] public string SelectedFile { get; set; }
        [Reactive] public ReactiveList<BoardToExport> Boards { get; set; }

        [Reactive] public bool ExportJson { get; set; }
        [Reactive] public bool ExportXml { get; set; }
        [Reactive] public bool ExportKamban { get; set; }
        [Reactive] public bool ExportXls { get; set; }
        [Reactive] public bool ExportPdf { get; set; }

        [Reactive] public string TargetFolder { get; set; }
        [Reactive] public string TargetFile { get; set; }

        [Reactive] public bool DatePostfix { get; set; }
        [Reactive] public bool SplitBoardsToFiles { get; set; }

        public ReactiveCommand<Unit, Unit> SelectTargetFolderCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ExportViewModel(IAppModel am)
        {
            appModel = am;

            var canExport = this.WhenAnyValue(x => x.SelectedFile, y => y.Boards.ItemChanged,
                (sf, br) => !string.IsNullOrEmpty(sf) && Boards.Where(z => z.IsChecked).Count() > 0);

            SelectTargetFolderCommand = ReactiveCommand.Create(SelectTargetFolderCommandExecute);
            ExportCommand = ReactiveCommand.CreateFromTask(ExportCommandExecute);
            CancelCommand = ReactiveCommand.Create(() => this.Close());

            // TODO: handle not found file

            this.ObservableForProperty(x => x.SelectedFile)
                .Subscribe(async (sf) =>
                {
                    prjService = appModel.LoadProjectService(sf.Value);
                    Boards.Clear();

                    var boards = (await prjService.GetAllBoardsInFileAsync())
                        .Select(x => new BoardToExport { Board = x, IsChecked = true });

                    Boards.AddRange(boards);

                    TargetFile = Path.GetFileNameWithoutExtension(sf.Value) + "_export";
                });

            this.ObservableForProperty(x => x.TargetFolder)
                .Subscribe(x =>
                {
                    appModel.ArchiveFolder = x.Value;
                    appModel.SaveConfig();
                });
        }

        private async Task ExportCommandExecute()
        {
            string fileName = TargetFolder + "\\" + TargetFile;
            if (DatePostfix)
                fileName += "_" + DateTime.Now.ToString("yyyyMMdd");

            // TODO: check target folder exists
            if (!SplitBoardsToFiles)
                await DoExportWhole(fileName);
            else
                await DoExportSplit(fileName);
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
                DoExportKamban(db, fileName + ".db");
        }

        private void DoExportJson(DatabaseToExport db, string fileName)
        {
            // TODO: check target file not exists

            string output = JsonConvert.SerializeObject(db, Formatting.Indented);
            File.WriteAllText(fileName, output);
        }

        private void DoExportKamban(DatabaseToExport db, string fileName)
        {
            // TODO: check target file not exists

            var prj = appModel.CreateProjectService(fileName);

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
            AvailableFiles = new ReactiveList<string>();
            Boards = new ReactiveList<BoardToExport>();

            AvailableFiles.AddRange(appModel.GetRecentDocuments());
            SelectedFile = AvailableFiles.Count > 0 ? AvailableFiles[0] : null;

            ExportJson = true;
            DatePostfix = true;
            SplitBoardsToFiles = false;

            var fi = new FileInfo(SelectedFile);

            TargetFolder = appModel.ArchiveFolder ?? fi.DirectoryName;
        }
    }
}
