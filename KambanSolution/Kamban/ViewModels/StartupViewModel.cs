using AutoMapper;
using DynamicData;
using DynamicData.Binding;
using Kamban.Core;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
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

        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Pinned { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Today { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> ThisMonth { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Older { get; private set; }

        public ReactiveCommand NewFileCommand { get; set; }
        public ReactiveCommand OpenFileCommand { get; set; }
        [Reactive] public ReactiveCommand<RecentViewModel, Unit> OpenRecentDbCommand { get; set; }
        public ReactiveCommand ExportCommand { get; set; }
        public ReactiveCommand ExitCommand { get; set; }

        [Reactive] public string Basement { get; set; }

        public StartupViewModel(IShell shell, IAppModel appModel, IDialogCoordinator dc,
            IMapper mp, IAppConfig cfg)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;
            dialCoord = dc;
            mapper = mp;
            appConfig = cfg;

            OpenRecentDbCommand = ReactiveCommand.Create<RecentViewModel>(async (rvm) =>
            {
                if (await OpenBoardView(rvm.Uri))
                    appConfig.UpdateRecent(rvm.Uri, rvm.Pinned);
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
                    var uri = dialog.FileName;

                    if (await OpenBoardView(uri))
                        appConfig.UpdateRecent(uri, false);
                }
            });

            ExportCommand = ReactiveCommand.Create(() => 
                this.shell.ShowView<ExportView>(), appModel.DbsCountMoreZero);

            ExitCommand = ReactiveCommand.Create(() => App.Current.Shutdown());

            Pinned = appConfig.RecentObservable
                .Filter(x => x.Pinned);

            var notPinned = appConfig.RecentObservable
                .Filter(x => !x.Pinned);

            Today = notPinned
                .Filter(x => x.LastAccess.IsToday());

            // TODO: Yesterday observer

            ThisMonth = notPinned
                .Filter(x => !x.LastAccess.IsToday() && x.LastAccess.IsThisMonth());

            Older = notPinned
                .Filter(x => !x.LastAccess.IsToday() && !x.LastAccess.IsThisMonth());

            // TODO: move autosaver to AppConfig
            
            appConfig.RecentObservable
                .WhenAnyPropertyChanged("Pinned")
                .Subscribe(x => appConfig.UpdateRecent(x.Uri, x.Pinned));

            var ver = Assembly.GetExecutingAssembly().GetName();
            Basement = $"2018 Kamban v{ver.Version.Major}.{ver.Version.Minor}";
        } //ctor

        private async Task<bool> OpenBoardView(string uri)
        {
            var file = new FileInfo(uri);

            if (!file.Exists)
            {
                appConfig.RemoveRecent(uri);
                appModel.RemoveDb(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was deleted or moved");
                return false;
            }

            var db = await appModel.LoadDb(uri);
            if (!db.Loaded)
            {
                appConfig.RemoveRecent(uri);
                appModel.RemoveDb(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                return false;
            }

            var title = file.FullName;

            await Task.Delay(200);

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, Db = db },
                options: new UiShowOptions { Title = title });

            return true;
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
        }

    }//end of classs

    public static class DateTimeExtensionMethods
    {
        public static bool IsSameMonth(this DateTime datetime1, DateTime datetime2)
        {
            return datetime1.Year == datetime2.Year
                && datetime1.Month == datetime2.Month;
        }

        public static bool IsSameDay(this DateTime datetime1, DateTime datetime2)
        {
            return datetime1.Year == datetime2.Year
                && datetime1.Month == datetime2.Month
                && datetime1.Day == datetime2.Day;
        }

        public static bool IsToday(this DateTime datetime1)
        {
            return IsSameDay(datetime1, DateTime.Now);
        }

        public static bool IsThisMonth(this DateTime datetime1)
        {
            return IsSameMonth(datetime1, DateTime.Now);
        }
    }
}
