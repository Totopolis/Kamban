using System;
using System.Reactive;
using System.Threading.Tasks;
using Kamban.Repository;
using Kamban.Repository.Models;
using Kamban.ViewModels.ImportScheme;
using Kamban.ViewRequests;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class ImportSchemeViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private ILoadRepository _repository;
        private BoxScheme _scheme;
        
        public BoxImportSchemeViewModel Scheme { get; set; }

        public ReactiveCommand<Unit, Unit> ImportCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ReloadCommand { get; set; }

        public ImportSchemeViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            ImportCommand = ReactiveCommand.CreateFromTask(Import);
            ReloadCommand = ReactiveCommand.CreateFromTask(ReloadScheme);
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
            //ToDo: add new tab with kamban
            await Task.CompletedTask;
        }

        private async Task ReloadScheme()
        {
            ProgressDialogController dialogController = null;
            try
            {
                dialogController = await _dialogCoordinator.ShowProgressAsync(this, "Scheme Loading", "Loading...");
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