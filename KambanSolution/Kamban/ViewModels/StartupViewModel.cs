using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AutoMapper;
using DynamicData;
using DynamicData.Binding;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class StartupViewModel : ViewModelBase, IInitializableViewModel
    {
        private readonly IShell shell;
        private readonly IAppModel appModel;
        private readonly IDialogCoordinator dialCoord;
        private readonly IMapper mapper;
        private readonly IAppConfig appConfig;

        public ReadOnlyObservableCollection<DbViewModel> Recents { get; set; }
        public ReactiveCommand NewFileCommand { get; set; }
        public ReactiveCommand OpenFileCommand { get; set; }
        public ReactiveCommand<string, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand ExportCommand { get; set; }
        public ReactiveCommand ExitCommand { get; set; }

        [Reactive]
        public bool IsLoading { get; set; }

        public StartupViewModel(IShell shell, IAppModel appModel, IDialogCoordinator dc,
            IMapper mp, IAppConfig cfg)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;
            dialCoord = dc;
            mapper = mp;
            appConfig = cfg;

            Recents = appModel.Dbs;

            OpenRecentDbCommand = ReactiveCommand.Create<string>(async (uri) =>
            {
                IsLoading = true;
                await OpenBoardView(uri);
                IsLoading = false;
            });

            NewFileCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<WizardView>(
                    new WizardViewRequest { ViewId = "Creating new file", Uri = null }));

            OpenFileCommand = ReactiveCommand.Create(async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = @"SQLite DataBase | *.kam",
                    Title = @"Select exists database"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    this.IsLoading = true;
                    var uri = dialog.FileName;
                    await OpenBoardView(uri);
                    this.IsLoading = false;
                }
            });

            ExportCommand = ReactiveCommand.Create(() => 
                this.shell.ShowView<ExportView>(), appModel.DbsCountMoreZero);

            ExitCommand = ReactiveCommand.Create(() => App.Current.Shutdown());

            this.IsLoading = false;
        } //ctor

        private async Task OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
            {
                appConfig.RemoveRecent(uri);
                appModel.RemoveDb(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was deleted or moved");
                return;
            }

            var db = await appModel.LoadDb(uri);
            if (!db.Loaded)
            {
                appConfig.RemoveRecent(uri);
                appModel.RemoveDb(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                return;
            }

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, Db = db },
                options: new UiShowOptions { Title = title });
        }

        public void Initialize(ViewRequest viewRequest)
        {
            shell.AddGlobalCommand("File", "New db", "NewFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "Open", "OpenFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Export", "ExportCommand", this, true)
                .SetHotKey(ModifierKeys.Control, Key.U);

            shell.AddGlobalCommand("File", "Exit", "ExitCommand", this);

            Observable.FromAsync(() => FillRecentsAsync())
                .ObserveOnDispatcher()
                .Subscribe();
        }

        private async Task FillRecentsAsync()
        {
            var toFill = appConfig.Recent;

            foreach (var uri in toFill)
                await appModel.LoadDb(uri);
        }

    }//end of classs
}
