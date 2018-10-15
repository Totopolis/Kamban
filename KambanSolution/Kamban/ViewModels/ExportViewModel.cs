using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
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
            ExportCommand = ReactiveCommand.Create(ExportCommandExecute);
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

        private void ExportCommandExecute()
        {

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
