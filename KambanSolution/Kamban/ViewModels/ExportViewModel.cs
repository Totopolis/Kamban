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
using System.Windows.Media;
using Autofac;
using AutoMapper;
using DynamicData;
using Kamban.Contracts;
using Kamban.Core;
using Kamban.Export.Options;
using Kamban.ViewModels.Core;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using PdfSharp;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

namespace Kamban.ViewModels
{
    public class BoardToExport : ReactiveObject
    {
        [Reactive] public BoardViewModel Board { get; set; }
        [Reactive] public bool IsChecked { get; set; }
    }

    public class ExportViewModel : ViewModelBase
    {
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private readonly IMapper mapper;
        private readonly IAppConfig _appConfig;
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
        [Reactive] public bool ShowCardIds { get; set; }
        [Reactive] public bool SwimLaneView { get; set; }
        [Reactive] public Color ColorTheme { get; set; }
        public ReactiveCommand<Unit, Unit> SelectTargetFolderCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

        public ExportViewModel(IShell sh, IAppModel am, IDialogCoordinator dc, 
            IAppConfig cfg, IMapper mapper)
        {
            shell = sh;
            appModel = am;
            dialCoord = dc;
            this.mapper = mapper;
            _appConfig = cfg;
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
            _appConfig.ColorThemeObservable
                .Subscribe(x => UpdateColorTheme());
        }
        private void UpdateColorTheme()
        {
            if (_appConfig.ColorTheme == Color.FromArgb(0, 255, 255, 255)) //transparent
                ColorTheme = Color.FromArgb(255, 255, 255, 255); //white as classical theme
            else
                ColorTheme = _appConfig.ColorTheme;
        }

        private async Task ExportCommandExecute()
        {
            if (!Directory.Exists(TargetFolder))
            {
                await dialCoord.ShowMessageAsync(this, "Error", "Target directory not found");
                return;
            }

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
            var jb = new Box();
            jb.Boards.AddRange(GetBoardsSelectedToExport());
            jb.Cards.AddRange(mapper.Map<IEnumerable<Card>>(SelectedBox.Cards.Items));
            jb.Rows.AddRange(mapper.Map<IEnumerable<Row>>(SelectedBox.Rows.Items));
            jb.Columns.AddRange(mapper.Map<IEnumerable<Column>>(SelectedBox.Columns.Items));

            await DoExportForNeededFormats(jb, fileName);
        }

        private async Task DoExportSplit(string fileName)
        {
            foreach (var brd in GetBoardsSelectedToExport())
            {
                var jb = new Box();
                jb.Boards.Add(brd);
                jb.Cards.AddRange(
                    mapper.Map<IEnumerable<Card>>(SelectedBox.Cards.Items.Where(x => x.BoardId == brd.Id)));
                jb.Rows.AddRange(
                    mapper.Map<IEnumerable<Row>>(SelectedBox.Rows.Items.Where(x => x.BoardId == brd.Id)));
                jb.Columns.AddRange(
                    mapper.Map<IEnumerable<Column>>(SelectedBox.Columns.Items.Where(x => x.BoardId == brd.Id)));

                await DoExportForNeededFormats(jb, fileName + "_" + brd.Name);
            }
        }

        private IEnumerable<Board> GetBoardsSelectedToExport()
        {
            var selectedBoardIds = new HashSet<int>(
                AvailableBoards
                    .Where(x => x.IsChecked)
                    .Select(x => x.Board.Id)
            );

            return mapper.Map<IEnumerable<Board>>(SelectedBox.Boards.Items)
                .Where(x => selectedBoardIds.Contains(x.Id));
        }

        private async Task DoExportForNeededFormats(Box box, string fileName)
        {
            var tasks = new List<Task>();
            if (ExportJson)
            {
                var export = shell.Container.ResolveNamed<IExportService>("json");
                tasks.Add(export.DoExport(box, fileName, null));
            }

            if (ExportKamban)
            {
                var export = shell.Container.ResolveNamed<IExportService>("kamban");
                tasks.Add(export.DoExport(box, fileName, null));
            }

            if (ExportXlsx)
            {
                var export = shell.Container.ResolveNamed<IExportService>("xlsx");
                tasks.Add(export.DoExport(box, fileName, null));
            }

            if (ExportPdf)
            {
                FixedDocument RenderToXps(Size size)
                {
                    var selectedBoardIds = new HashSet<int>(box.Boards.Select(x => x.Id));
                    return ((ShellEx)shell).ViewsToDocument<BoardEditForExportView>(
                        SelectedBox.Boards.Items
                            .Where(x => selectedBoardIds.Contains(x.Id))
                            .Select(x => new BoardViewRequest
                            {
                                ViewId = SelectedBox.Uri,
                                ShowCardIds = ShowCardIds,
                                SwimLaneView = SwimLaneView,
                                Box = SelectedBox,
                                Board = x
                            })
                            .Cast<ViewRequest>()
                            .ToArray(),
                        size, PdfOptions.ScaleOptions);
                }

                var export = shell.Container.ResolveNamed<IExportService>("pdf");

                var opts = new Tuple<Func<Size, FixedDocument>, PdfOptions>(RenderToXps, PdfOptions);

                tasks.Add(export.DoExport(box, fileName, opts));
            }

            if (tasks.Any())
                await Task.WhenAll(tasks.ToArray());
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
    }
}
