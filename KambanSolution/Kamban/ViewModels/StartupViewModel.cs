using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
        public ReactiveCommand OpenFileCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand<string, Unit> RemoveRecentCommand { get; set; }
        public ReactiveCommand<string, Unit> AccentChangeCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; set; }

        [Reactive]
        public bool IsLoading { get; set; }

        private readonly IShell shell;
        private readonly IAppModel appModel;

        public StartupViewModel(IShell shell, IAppModel appModel)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;
            Recents = new ReactiveList<string>();

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

            var canExport = Recents.CountChanged.Select(x => x > 1);
            ExportCommand = ReactiveCommand.Create(() => this.shell.ShowView<ExportView>(), canExport);

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

            IProjectService service = await Task.Run(() => appModel.LoadProjectService(uri));

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, PrjService = service },
                options: new UiShowOptions { Title = title });

            AddRecent(uri);

            return true;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            shell.AddGlobalCommand("File", "New db", "NewFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "Open", "OpenFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Export", "ExportCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.U);

            shell.AddGlobalCommand("File", "Exit", null, this);

            this.appModel.LoadConfig();

            var rcnts = this.appModel.GetRecentDocuments();
            Recents.Clear();
            Recents.AddRange(rcnts.Take(3));
        }
    }
}
