using System.Reactive;
using System.Threading.Tasks;
using System.Windows;
using Kamban.Repository.Redmine;
using Kamban.Views.Dialogs;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class ImportViewModel : ViewModelBase
    {
        public ReactiveCommand<Unit, Unit> ImportRedmineCommand { get; set; }

        public ImportViewModel()
        {
            ImportRedmineCommand = ReactiveCommand.CreateFromTask(ShowRedmineImport);
        }

        private async Task ShowRedmineImport()
        {
            var loginData = await LoginToRedmine();
            if (loginData != null)
            {
                var repo = new RedmineRepository(loginData.Host, loginData.Username, loginData.Password);
                var scheme = await repo.LoadScheme();
                // ToDo: show scheme
            }
        }

        private async Task<LoginWithUrlDialogData> LoginToRedmine()
        {
            var view = Application.Current.MainWindow as MetroWindow;

            var settings = new LoginWithUrlDialogSettings
            {
                AnimateShow = true,
                AnimateHide = true,
                AffirmativeButtonText = "Login",
                NegativeButtonText = "Cancel",
                NegativeButtonVisibility = Visibility.Visible,
                EnablePasswordPreview = true,
                RememberCheckBoxVisibility = Visibility.Collapsed
            };

            var loginDialog = new LoginWithUrlDialog(view, settings) { Title = "Login to Redmine" };
            await view.ShowMetroDialogAsync(loginDialog);
            var result = await loginDialog.WaitForButtonPressAsync();
            await view.HideMetroDialogAsync(loginDialog);
            return result;
        }

    }
}