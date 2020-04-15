﻿using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Kamban.Core;
using Kamban.Repository.Redmine;
using Kamban.ViewRequests;
using Kamban.Views;
using Kamban.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class ImportViewModel : ViewModelBase
    {
        private IShell _shell;
        private IDialogCoordinator _dialogCoordinator;
        private readonly IAppConfig _appConfig;

        [Reactive] public bool LoadFullScheme { get; set; }
        public ReactiveCommand<Unit, Unit> ImportRedmineCommand { get; set; }

        public ImportViewModel(IShell shell, IDialogCoordinator dialogCoordinator,
            IAppConfig appCfg)
        {
            _shell = shell;
            _dialogCoordinator = dialogCoordinator;
            _appConfig = appCfg;

            ImportRedmineCommand = ReactiveCommand.CreateFromTask(ShowRedmineImport);
        }

        private async Task ShowRedmineImport()
        {
            var loginData = await LoginToRedmine();
            if (loginData == null)
                return;

            try
            {
                var repo = new RedmineRepository(loginData.Host, loginData.Username, loginData.Password);
                _shell.ShowView(
                    scope => scope.Resolve<ImportSchemeView>(new NamedParameter("loadAll", LoadFullScheme)),
                    new ImportSchemeViewRequest
                    {
                        ViewId = $"{loginData.Host}?u={loginData.Username}&p={loginData.SecurePassword.GetHashCode()}",
                        Repository = repo
                    },
                    new UiShowOptions
                    {
                        Title = "Redmine Import"
                    });
            }
            catch (Exception e)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        private async Task<LoginWithUrlDialogData> LoginToRedmine()
        {
            var settings = new LoginWithUrlDialogSettings
            {
                InitialHost = _appConfig.LastRedmineUrl,
                InitialUsername = _appConfig.LastRedmineUser,
                AnimateShow = true,
                AnimateHide = true,
                AffirmativeButtonText = "Login",
                NegativeButtonText = "Cancel",
                NegativeButtonVisibility = Visibility.Visible,
                EnablePasswordPreview = true,
                RememberCheckBoxVisibility = Visibility.Collapsed
            };

            var loginDialog = new LoginWithUrlDialog(null, settings) { Title = "Login to Redmine" };
            await _dialogCoordinator.ShowMetroDialogAsync(this, loginDialog);
            var result = await loginDialog.WaitForButtonPressAsync();
            await _dialogCoordinator.HideMetroDialogAsync(this, loginDialog);

            if (result != null)
            {
                _appConfig.LastRedmineUrl = result.Host;
                _appConfig.LastRedmineUser = result.Username;
            }

            return result;
        }
    }
}
