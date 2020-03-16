using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kamban.Common;
using Kamban.Core;
using Kamban.Repository;
using Kamban.Contracts;
using Kamban.ViewModels.Core;
using Kamban.ViewModels.ImportScheme;
using Kamban.ViewRequests;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class ImportSchemeViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell _shell;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IAppModel _appModel;
        private readonly IAppConfig _appConfig;

        private ILoadRepository _repository;
        private BoxScheme _scheme;
        private bool _loaded;

        public bool LoadAll { get; set; }
        [Reactive] public bool CanReloadPart { get; set; }
        public BoxImportSchemeViewModel Scheme { get; set; }

        [Reactive] public string FileName { get; set; }
        [Reactive] public string FolderName { get; set; }

        [Reactive] public bool DontImportUnusedRows { get; set; }
        [Reactive] public bool DontImportUnusedColumns { get; set; }

        public ReactiveCommand<Unit, Unit> ImportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ReloadPartCommand { get; set; }
        public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; set; }

        public ImportSchemeViewModel(IShell shell, IDialogCoordinator dialogCoordinator, IAppModel appModel, IAppConfig appConfig)
        {
            _shell = shell;
            _dialogCoordinator = dialogCoordinator;
            _appModel = appModel;
            _appConfig = appConfig;
            ImportCommand = ReactiveCommand.CreateFromTask(Import);
            ReloadCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (!_loaded)
                {
                    await ReloadScheme();
                    _loaded = true;
                }
            });
            ReloadPartCommand = ReactiveCommand.CreateFromTask(ReloadSchemePart,
                this.WhenAnyValue(x => x.CanReloadPart));

            FolderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FileName = $"_tmp_{DateTime.UtcNow.Ticks}.kam";
            SelectFolderCommand = ReactiveCommand.Create(() =>
            {
                using (var dialog = new FolderBrowserDialog
                {
                    ShowNewFolderButton = false,
                    SelectedPath = FolderName
                }) {

                    if (dialog.ShowDialog() == DialogResult.OK)
                        FolderName = dialog.SelectedPath;
                }
            });

            Scheme = new BoxImportSchemeViewModel();
        }

        public void Initialize(ViewRequest viewRequest)
        {
            if (!(viewRequest is ImportSchemeViewRequest importSchemeViewRequest))
                throw new ArgumentException($"Cannot initialize with request of type {viewRequest.GetType().Name}");

            _repository = importSchemeViewRequest.Repository;
        }

        private async Task Import()
        {
            ProgressDialogController dialogController = null;
            try
            {
                var uri = $"{FolderName}\\{FileName}";

                if (!Directory.Exists(FolderName))
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", "Directory does not exists");
                    return;
                }
                if (File.Exists(uri))
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", "File already exists");
                    return;
                }

                if (!Scheme.IsSchemeValid())
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error",
                        "Selected Scheme is not valid\nRows or Columns are empty");
                    return;
                }

                dialogController = await _dialogCoordinator.ShowProgressAsync(this, "Cards Loading", "Loading...");
                dialogController.SetIndeterminate();

                var filter = Scheme.GetCardFilter();
                var cards = await _repository.LoadCards(filter);

                cards.ForEach(x => x.Color = DefaultColorItems.LemonChiffon.SystemName);

                // Fill order numbers inside each cell
                cards
                    .GroupBy(x => new { x.RowId, x.ColumnId })
                    .ToList()
                    .ForEach(cell =>
                    {
                        int i = -10;
                        cell.ToList().ForEach(y => y.Order = i += 10);
                    });

                var columns = _scheme.Columns.Where(x => filter.BoardIds.Contains(x.BoardId) && filter.ColumnIds.Contains(x.Id));
                if (DontImportUnusedColumns)
                    columns = columns.Where(x => cards.Exists(y => y.ColumnId == x.Id));

                var rows = _scheme.Rows.Where(x => filter.BoardIds.Contains(x.BoardId) && filter.RowIds.Contains(x.Id));
                if (DontImportUnusedRows)
                    rows = rows.Where(x => cards.Exists(y => y.RowId == x.Id));

                var boxViewModel = await _appModel.Create(uri);
                boxViewModel.Load(new Box
                {
                    Boards = _scheme.Boards.Where(x => filter.BoardIds.Contains(x.Id)).ToList(),
                    Columns = columns.ToList(),
                    Rows = rows.ToList(),
                    Cards = cards
                });

                await dialogController.CloseAsync();

                _appConfig.UpdateRecent(uri, false);
                _shell.ShowView<BoardView>(
                    viewRequest: new BoardViewRequest { ViewId = uri, Box = boxViewModel },
                    options: new UiShowOptions { Title = "*" });
            }
            catch (Exception e)
            {
                if (dialogController != null)
                {
                    await dialogController.CloseAsync();
                }

                await _dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        private async Task ReloadScheme()
        {
            ProgressDialogController dialogController = null;
            try
            {
                dialogController = await _dialogCoordinator.ShowProgressAsync(this, "Scheme Loading", "Loading...");
                dialogController.SetIndeterminate();

                if (LoadAll)
                {
                    _scheme = await _repository.LoadScheme();
                }
                else
                {
                    CanReloadPart = true;
                    _scheme = new BoxScheme
                    {
                        Boards = await _repository.LoadSchemeBoards()
                    };
                }

                Scheme.Update(_scheme);
                await dialogController.CloseAsync();
            }
            catch (Exception e)
            {
                if (dialogController != null)
                {
                    await dialogController.CloseAsync();
                }

                await _dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);

                Close();
            }
        }

        private async Task ReloadSchemePart()
        {
            if (!CanReloadPart)
                return;

            if (!Scheme.Boards.Any(x => x.IsSelected))
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Nothing to load", "Boards are empty");
                return;
            }

            ProgressDialogController dialogController = null;
            try
            {
                dialogController = await _dialogCoordinator.ShowProgressAsync(this, "Scheme Loading", "Loading...");
                dialogController.SetIndeterminate();

                var boardIds = Scheme.Boards.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
                foreach (var board in Scheme.Boards)
                    board.IsEnabled = board.IsSelected;

                var columnsTask = _repository.LoadSchemeColumns(boardIds);
                var rowsTask = _repository.LoadSchemeRows(boardIds);
                _scheme.Columns = await columnsTask;
                _scheme.Rows = await rowsTask;

                Scheme.UpdateColumns(_scheme.Columns);
                Scheme.UpdateRows(_scheme.Rows);

                CanReloadPart = false;

                await dialogController.CloseAsync();
            }
            catch (Exception e)
            {
                if (dialogController != null)
                {
                    await dialogController.CloseAsync();
                }

                await _dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
        }
    }
}