using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
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
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private IProjectService prjService;
        private SourceList<BoardToExport> boards;

        [Reactive] public ReadOnlyObservableCollection<DbViewModel> AvailableFiles { get; set; }
        [Reactive] public DbViewModel SelectedFile { get; set; }
        [Reactive] public ReadOnlyObservableCollection<BoardToExport> AvailableBoards { get; set; }

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
        public ReactiveCommand CancelCommand { get; set; }

        public ExportViewModel(IShell shell, IAppModel am, IDialogCoordinator dc)
        {
            this.shell = shell as IShell;
            this.appModel = am;
            dialCoord = dc;

            boards = new SourceList<BoardToExport>();

            appModel.RecentsDb
                .Connect()
                .AutoRefresh()
                .Sort(SortExpressionComparer<DbViewModel>.Descending(x => x.LastAccess))
                //.Top(3) // ?
                .Bind(out ReadOnlyObservableCollection<DbViewModel> temp)
                .Subscribe();

            AvailableFiles = temp;
            AvailableBoards = boards.SpawnCollection();

            var canExport = boards
                .Connect()
                .AutoRefresh()
                .Select(x => boards.Items.Count(y => y.IsChecked) > 0 && !string.IsNullOrEmpty(SelectedFile.Uri));

            SelectTargetFolderCommand = ReactiveCommand.Create(SelectTargetFolderCommandExecute);
            ExportCommand = ReactiveCommand.CreateFromTask(ExportCommandExecute, canExport);
            CancelCommand = ReactiveCommand.Create(() => this.Close());

            // TODO: handle not found file

            this.ObservableForProperty(x => x.SelectedFile)
                .Subscribe(async (sf) =>
                {
                    prjService = appModel.LoadProjectService(sf.Value.Uri);

                    var lst = (await prjService.GetAllBoardsInFileAsync())
                        .Select(x => new BoardToExport { Board = x, IsChecked = true });

                    boards.ClearAndAddRange(lst);

                    TargetFile = Path.GetFileNameWithoutExtension(sf.Value.Uri) + "_export";
                });

            this.ObservableForProperty(x => x.TargetFolder)
                .Subscribe(x => appModel.ArchiveFolder = x.Value);
        }

        private async Task ExportCommandExecute()
        {
            if (!Directory.Exists(TargetFolder))
            {
                await dialCoord.ShowMessageAsync(this, "Warning", "Target directory not found");
                return;
            }

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

            SelectedFile = AvailableFiles.FirstOrDefault();

            ExportJson = true;
            DatePostfix = true;
            SplitBoardsToFiles = false;

            var fi = new FileInfo(SelectedFile.Uri);

            TargetFolder = appModel.ArchiveFolder ?? fi.DirectoryName;
        }
    }
}
