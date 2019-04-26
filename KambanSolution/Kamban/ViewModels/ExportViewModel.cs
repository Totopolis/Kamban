using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using DynamicData;
using Kamban.Core;
using Kamban.Export;
using Kamban.Export.Options;
using Kamban.Repository;
using Kamban.ViewModels.Core;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using PdfSharp;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class BoardToExport : ReactiveObject
    {
        [Reactive] public BoardViewModel Board { get; set; }
        [Reactive] public bool IsChecked { get; set; }
    }

    public class ExportViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private readonly IExportService export;
        private IProjectService prjService;
        private SourceList<BoardToExport> boards;

        [Reactive] public ReadOnlyObservableCollection<BoxViewModel> AvailableBoxes { get; set; }
        [Reactive] public BoxViewModel SelectedBox { get; set; }
        [Reactive] public ReadOnlyObservableCollection<BoardToExport> AvailableBoards { get; set; }

        [Reactive] public bool ExportJson { get; set; }
        [Reactive] public bool ExportXml { get; set; }
        [Reactive] public bool ExportKamban { get; set; }
        [Reactive] public bool ExportXlsx { get; set; }
        [Reactive] public bool ExportPdf { get; set; }

        // Pdf settings
        public PdfOptions PdfOptions { get; set; }
        public PdfOptionsAvailable PdfOptionsAvailable { get; set; }

        [Reactive] public string TargetFolder { get; set; }
        [Reactive] public string TargetFile { get; set; }

        [Reactive] public bool DatePostfix { get; set; }
        [Reactive] public bool SplitBoardsToFiles { get; set; }

        public ReactiveCommand<Unit, Unit> SelectTargetFolderCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ExportViewModel(IShell sh, IAppModel am, IDialogCoordinator dc, IAppConfig cfg, IExportService ex)
        {
            shell = sh;
            appModel = am;
            dialCoord = dc;
            export = ex;

            AvailableBoxes = appModel.Boxes;

            boards = new SourceList<BoardToExport>();
            AvailableBoards = boards.SpawnCollection();

            ExportJson = true;
            DatePostfix = true;
            SplitBoardsToFiles = false;

            PdfOptionsAvailable = new PdfOptionsAvailable
            {
                PageSizes = Enum.GetValues(typeof(PageSize)).Cast<PageSize>().ToArray(),
                PageOrientations = Enum.GetValues(typeof(PageOrientation)).Cast<PageOrientation>().ToArray(),
                ScaleFittings = Enum.GetValues(typeof(ScaleFitting)).Cast<ScaleFitting>().ToArray()
            };
            PdfOptions = new PdfOptions
            {
                PageSize = PageSize.A4,
                PageOrientation = PageOrientation.Portrait,
                ScaleOptions = new ScaleOptions
                {
                    Padding = new Thickness(),
                    ScaleToFit = true,
                    ScaleFitting = ScaleFitting.BothDirections,
                    MaxScale = 1.0,
                    MinScale = 0.0
                }
            };

            var canExport = boards
                .Connect()
                .AutoRefresh()
                .Filter(x => x.IsChecked)
                .Select(x => AvailableBoards.Count(y => y.IsChecked) > 0
                             && !string.IsNullOrEmpty(SelectedBox.Uri) && File.Exists(SelectedBox.Uri));

            ExportCommand = ReactiveCommand.CreateFromTask(ExportCommandExecute, canExport);
            SelectTargetFolderCommand = ReactiveCommand.Create(SelectTargetFolderCommandExecute);
            CancelCommand = ReactiveCommand.Create(Close);

            this.ObservableForProperty(x => x.SelectedBox)
                .Where(x => x.Value != null)
                .Select(x => x.Value)
                .Subscribe(box =>
                {
                    boards.ClearAndAddRange(box.Boards.Items
                        .Select(x => new BoardToExport {Board = x, IsChecked = true}));

                    TargetFile = Path.GetFileNameWithoutExtension(box.Uri) + "_export";
                });

            this.ObservableForProperty(x => x.TargetFolder)
                .Subscribe(x => cfg.ArchiveFolder = x.Value);

            SelectedBox = AvailableBoxes.First();

            var fi = new FileInfo(SelectedBox.Uri);
            TargetFolder = cfg.ArchiveFolder ?? fi.DirectoryName;
        }

        private async Task ExportCommandExecute()
        {
            if (!Directory.Exists(TargetFolder))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Target directory not found");
                return;
            }

            prjService = appModel.GetProjectService(SelectedBox.Uri);

            string fileName = TargetFolder + "\\" + TargetFile;
            if (DatePostfix)
                fileName += "_" + DateTime.Now.ToString("yyyyMMdd-hhmmss");

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
            var jb = new BoxToExport();
            jb.BoardList.AddRange(boardsSelected);

            foreach (var brd in jb.BoardList)
            {
                var columns = await prjService.Repository.GetColumns(brd.Id);
                jb.ColumnList.AddRange(columns);

                var rows = await prjService.Repository.GetRows(brd.Id);
                jb.RowList.AddRange(rows);

                var cards = await prjService.Repository.GetCards(brd.Id);
                jb.CardList.AddRange(cards);
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
                var jb = new BoxToExport();
                jb.BoardList.Add(brd);

                var columns = await prjService.Repository.GetColumns(brd.Id);
                jb.ColumnList.AddRange(columns);
                var rows = await prjService.Repository.GetRows(brd.Id);
                jb.RowList.AddRange(rows);
                var cards = await prjService.Repository.GetCards(brd.Id);
                jb.CardList.AddRange(cards);

                // 2. export
                DoExportForNeededFormats(jb, fileName + "_" + brd.Name);
            }
        }

        private async Task<IEnumerable<Board>> GetBoardsSelectedToExport()
        {
            var selectedBoardIds = new HashSet<int>(
                AvailableBoards
                    .Where(x => x.IsChecked)
                    .Select(x => x.Board.Id)
            );

            var boardsAll = await prjService.Repository.GetAllBoards();
            return boardsAll.Where(x => selectedBoardIds.Contains(x.Id));
        }

        private void DoExportForNeededFormats(BoxToExport box, string fileName)
        {
            if (ExportJson)
                export.ToJson(box, fileName);

            if (ExportKamban)
                export.ToKamban(box, fileName);

            if (ExportXlsx)
                export.ToXlsx(box, fileName);

            if (ExportPdf)
            {
                FixedDocument RenderToXps(Size size)
                {
                    var selectedBoardIds = new HashSet<int>(box.BoardList.Select(x => x.Id));
                    return ((ShellEx) shell).ViewsToDocument<BoardForExportView>(
                        SelectedBox.Boards.Items
                            .Where(x => selectedBoardIds.Contains(x.Id))
                            .Select(x => new BoardViewRequest {ViewId = SelectedBox.Uri, Box = SelectedBox, Board = x})
                            .Cast<ViewRequest>()
                            .ToArray(),
                        size, PdfOptions.ScaleOptions);
                }

                export.ToPdf(box, RenderToXps, fileName, PdfOptions);
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
        }
    }
}