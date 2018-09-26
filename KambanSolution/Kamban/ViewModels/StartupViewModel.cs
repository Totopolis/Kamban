using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;
using Application = System.Windows.Application;

namespace Kamban.ViewModels
{
    public class StartupViewModel : ViewModelBase, IInitializableViewModel
    {
        public ReactiveList<string> Recents { get; set; }
        public ReactiveCommand NewFileCommand { get; set; }
        public ReactiveCommand NewBoardCommand { get; set; }
        public ReactiveCommand OpenFileCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand<string, Unit> RemoveRecentCommand { get; set; }
        public ReactiveCommand<string, Unit> AccentChangeCommand { get; set; }

        [Reactive]
        public bool IsLoading { get; set; }

        private readonly IShell shell;
        private readonly IAppModel appModel;

        public StartupViewModel(IShell shell, IAppModel appModel)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;

            this.appModel.LoadConfig();

            var recent = this.appModel.GetRecentDocuments();
            Recents = new ReactiveList<string>(recent.Take(3));

            AccentChangeCommand =
                ReactiveCommand.Create<string>(color =>
                    ThemeManager.ChangeAppStyle(Application.Current,
                        ThemeManager.GetAccent(color),
                        ThemeManager.GetAppTheme("baselight")));

            OpenRecentDbCommand = ReactiveCommand.Create<string>(async (uri) =>
            {
                IsLoading = true;

                if (!await OpenBoardView(uri))
                {
                    RemoveRecent(uri);

                    await DialogCoordinator.Instance.ShowMessageAsync(this, "Error",
                        "File was deleted or moved");
                }

                IsLoading = false;
            });

            RemoveRecentCommand = ReactiveCommand.Create<string>(RemoveRecent);

            NewFileCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<WizardView>(
                    new WizardViewRequest { ViewId = "Creating new file", InExistedFile = false }));

            NewBoardCommand = ReactiveCommand.Create(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"Kamban DataBase | *.db",
                    Title = @"Select exists database"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var uri = dialog.FileName;

                    AddRecent(uri);
                    this.shell.ShowView<WizardView>(new WizardViewRequest
                    {
                        ViewId = $"Creating new board in {uri}",
                        InExistedFile = true,
                        Uri = uri
                    });
                }
            });

            OpenFileCommand = ReactiveCommand.Create(async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"SQLite DataBase | *.db",
                    Title = @"Select exists database"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.IsLoading = true;

                    var uri = dialog.FileName;
                    await OpenBoardView(uri);
                    AddRecent(uri);

                    this.IsLoading = false;
                }
            });

            this.IsLoading = false;
        } //ctor

        private void RemoveRecent(string uri)
        {
            appModel.RemoveRecent(uri);
            appModel.SaveConfig();
            Recents.PublishCollection(appModel.GetRecentDocuments().Take(3));
        }

        private void AddRecent(string uri)
        {
            appModel.AddRecent(uri);
            appModel.SaveConfig();

            Recents.Remove(uri);
            Recents.Insert(0, uri);
            if (Recents.Count > 3)
                Recents.RemoveAt(3);
        }

        private async Task<bool> OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
                return false;

            IScopeModel scope = await Task.Run(() => appModel.LoadScope(uri));

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, Scope = scope },
                options: new UiShowOptions { Title = title });

            AddRecent(uri);

            return true;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            shell.AddGlobalCommand("File", "New db", "NewFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "New board", "NewBoardCommand", this)
                .SetHotKey(ModifierKeys.Control | ModifierKeys.Shift, Key.N);

            shell.AddGlobalCommand("File", "Open", "OpenFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Exit", null, this);
        }
    }
}
