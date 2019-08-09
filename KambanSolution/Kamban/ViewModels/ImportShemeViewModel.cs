using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kamban.Core;
using Kamban.Repository;
using Kamban.Repository.Models;
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
        
        public BoxImportSchemeViewModel Scheme { get; set; }

        [Reactive] public string FileName { get; set; }
        [Reactive] public string FolderName { get; set; }

        public ReactiveCommand<Unit, Unit> ImportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; set; }
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
                var boxViewModel = await _appModel.Create(uri);
                boxViewModel.Load(new Box
                {
                    Boards = _scheme.Boards.Where(x => filter.BoardIds.Contains(x.Id)).ToList(),
                    Columns = _scheme.Columns.Where(x => filter.BoardIds.Contains(x.BoardId) && filter.ColumnIds.Contains(x.Id)).ToList(),
                    Rows = _scheme.Rows.Where(x => filter.BoardIds.Contains(x.BoardId) && filter.RowIds.Contains(x.Id)).ToList(),
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
                _scheme = await _repository.LoadScheme();
                Scheme.Reload(_scheme);
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
    }
}