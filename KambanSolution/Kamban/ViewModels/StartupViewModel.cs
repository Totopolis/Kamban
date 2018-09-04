using System.IO;
using System.Linq;
using System.Reactive;
using System.Windows.Forms;
using Kamban.Models;
using Kamban.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;
using Application = System.Windows.Application;

namespace Kamban.ViewModels
{
    public class StartupViewModel : ViewModelBase
    {
        public ReactiveList<string> BaseList { get; set; }
        public ReactiveCommand NewFileCommand { get; set; }
        public ReactiveCommand NewBoardCommand { get; set; }
        public ReactiveCommand OpenFileCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand<string, Unit> RemoveRecentCommand { get; set; }
        public ReactiveCommand<string, Unit> AccentChangeCommand { get; set; }

        private readonly IDistinctShell shell;
        private readonly IAppModel appModel;

        public StartupViewModel(IShell shell, IAppModel appModel)
        {
            this.shell = shell as IDistinctShell;
            this.appModel = appModel;

            this.appModel.LoadConfig();

            var recent = this.appModel.GetRecentDocuments();
            BaseList = new ReactiveList<string>(recent.Take(3));

            AccentChangeCommand =
                ReactiveCommand.Create<string>(color=> 
                    ThemeManager.ChangeAppStyle(Application.Current,
                        ThemeManager.GetAccent(color),
                        ThemeManager.GetAppTheme("baselight")));

            OpenRecentDbCommand = ReactiveCommand.Create<string>(uri =>
            {
                if (OpenBoardView(uri)) return;

                RemoveRecent(uri);
                DialogCoordinator.Instance.ShowMessageAsync(this, "Ошибка",
                    "Файл был удалён или перемещён из данной папки");
            });

            RemoveRecentCommand = ReactiveCommand.Create<string>(RemoveRecent);

            NewFileCommand = ReactiveCommand.Create(() =>
                this.shell.ShowDistinctView<WizardView>("Creating new file", new WizardViewRequest {InExistedFile = false}));

            NewBoardCommand = ReactiveCommand.Create(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"SQLite DataBase | *.db",
                    Title = @"Выбор существующего файла базы данных"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var uri = dialog.FileName;

                    AddRecent(uri);
                    this.shell.ShowDistinctView<WizardView>($"Creating new board in {uri}", new WizardViewRequest
                    {
                        InExistedFile = true,
                        Uri = uri
                    });
                }
            });

            OpenFileCommand = ReactiveCommand.Create(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"SQLite DataBase | *.db",
                    Title = @"Выбор существующего файла базы данных"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var uri = dialog.FileName;
                    OpenBoardView(uri);
                    AddRecent(uri);
                }
            });

        } //ctor

        private void RemoveRecent(string uri)
        {
            appModel.RemoveRecent(uri);
            appModel.SaveConfig();
            BaseList.PublishCollection(appModel.GetRecentDocuments().Take(3));
        }

        private void AddRecent(string uri)
        {
            appModel.AddRecent(uri);
            appModel.SaveConfig();

            BaseList.Remove(uri);
            BaseList.Insert(0, uri);
            if (BaseList.Count > 3)
                BaseList.RemoveAt(3);
        }

        private bool OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
                return false;

            var scope = appModel.LoadScope(uri);

            var title = file.FullName;

            shell.ShowDistinctView<BoardView>(title,
                viewRequest: new BoardViewRequest {Scope = scope},
                options: new UiShowOptions {Title = title});

            AddRecent(uri);

            return true;
        }
    }
}
