using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Autofac;
using Kamban.Core;
using Kamban.Repository.Redmine;
using Kamban.ViewRequests;
using Kamban.Views;
using Kamban.Views.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

namespace Kamban.ViewModels
{
    public class ImportViewModel : ViewModelBase
    {
        private IShell shell;
        private IDialogCoordinator dialCoord;
        private readonly IAppConfig appConfig;

        [Reactive] public bool LoadFullScheme { get; set; }
        [Reactive] public Color ColorTheme { get; set; }
        public ReactiveCommand<Unit, Unit> ImportRedmineCommand { get; set; }

        public ImportViewModel(IShell sh, IDialogCoordinator dc, IAppConfig ac)
        {
            shell = sh;
            dialCoord = dc;
            appConfig = ac;
            
            ImportRedmineCommand = ReactiveCommand.CreateFromTask(ShowRedmineImport);

            appConfig.ColorThemeObservable
                .Subscribe(x => ColorTheme = x);
        }

        private async Task ShowRedmineImport()
        {
            var loginData = await LoginToRedmine();
            if (loginData == null)
                return;

            try
            {
                var repo = new RedmineRepository(loginData.Host, loginData.Username, loginData.Password);
                shell.ShowView(
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
                await dialCoord.ShowMessageAsync(this, "Error", e.Message);
            }
        }

        private async Task<LoginWithUrlDialogData> LoginToRedmine()
        {
            var settings = new LoginWithUrlDialogSettings
            {
                InitialHost = appConfig.LastRedmineUrl,
                InitialUsername = appConfig.LastRedmineUser,
                AnimateShow = true,
                AnimateHide = true,
                AffirmativeButtonText = "Login",
                NegativeButtonText = "Cancel",
                NegativeButtonVisibility = Visibility.Visible,
                EnablePasswordPreview = true,
                RememberCheckBoxVisibility = Visibility.Collapsed
            };

            var loginDialog = new LoginWithUrlDialog(null, settings) { Title = "Login to Redmine" };
            await dialCoord.ShowMetroDialogAsync(this, loginDialog);
            var result = await loginDialog.WaitForButtonPressAsync();
            await dialCoord.HideMetroDialogAsync(this, loginDialog);

            if (result != null)
            {
                appConfig.LastRedmineUrl = result.Host;
                appConfig.LastRedmineUser = result.Username;
            }

            return result;
        }
    }
}
