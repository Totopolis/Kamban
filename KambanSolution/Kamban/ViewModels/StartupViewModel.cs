using AutoMapper;
using DynamicData;
using DynamicData.Binding;
using Kamban.Core;
using Kamban.Model;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
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
        private readonly IMonik mon;
        private bool initialized;

        public const string StartupViewId = "KambanStartupView";

        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Pinned { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Today { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Yesterday { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> ThisMonth { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Older { get; private set; }

        public ReactiveCommand NewFileCommand { get; private set; }
        public ReactiveCommand OpenFileCommand { get; private set; }
        [Reactive] public ReactiveCommand<RecentViewModel, Unit> OpenRecentDbCommand { get; private set; }
        public ReactiveCommand ExportCommand { get; private set; }
        public ReactiveCommand ShowStartupCommand { get; private set; }
        public ReactiveCommand ExitCommand { get; private set; }

        [Reactive] public string GetStarted { get; set; }
        [Reactive] public string Basement { get; set; }
        [Reactive] public ReadOnlyObservableCollection<PublicBoardJson> PublicBoards { get; set; }

        [Reactive] public ReactiveCommand<PublicBoardJson, Unit> OpenPublicBoardCommand { get; set; }

        public StartupViewModel(IShell shell, IAppModel appModel, IDialogCoordinator dc,
            IMapper mp, IAppConfig cfg, IMonik m)
        {
            this.shell = shell as IShell;
            this.appModel = appModel;
            dialCoord = dc;
            mapper = mp;
            appConfig = cfg;
            mon = m;

            initialized = false;

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

            OpenPublicBoardCommand = ReactiveCommand
                .CreateFromTask<PublicBoardJson>(OpenPublicBoardCommandExecute);

            ExportCommand = ReactiveCommand.Create(() => 
                this.shell.ShowView<ExportView>(), appModel.DbsCountMoreZero);

            ShowStartupCommand = ReactiveCommand.Create(() =>
            {
                shell.ShowView<StartupView>(
                    viewRequest: new ViewRequest { ViewId = StartupViewModel.StartupViewId },
                    options: new UiShowOptions { Title = "Start Page", CanClose = false });
            });

            ExitCommand = ReactiveCommand.Create(() => App.Current.Shutdown());

            Pinned = appConfig.RecentObservable
                .Filter(x => x.Pinned);

            var notPinned = appConfig.RecentObservable
                .Filter(x => !x.Pinned);

            Today = notPinned
                .Filter(x => x.LastAccess.IsToday());

            Yesterday = notPinned
                .Filter(x => x.LastAccess.IsYesterday());

            ThisMonth = notPinned
                .Filter(x => !x.LastAccess.IsToday() && !x.LastAccess.IsYesterday() && x.LastAccess.IsThisMonth());

            Older = notPinned
                .Filter(x => !x.LastAccess.IsToday() && !x.LastAccess.IsYesterday() && !x.LastAccess.IsThisMonth());

            // TODO: move autosaver to AppConfig
            
            appConfig.RecentObservable
                .WhenAnyPropertyChanged("Pinned")
                .Subscribe(x => appConfig.UpdateRecent(x.Uri, x.Pinned));

            appConfig.GetStarted.Subscribe(x => GetStarted = x);

            var ver = Assembly.GetExecutingAssembly().GetName();
            appConfig.Basement
                .Subscribe(x => Basement = x + $"v{ver.Version.Major}.{ver.Version.Minor}");
        } //ctor

        public async Task OpenPublicBoardCommandExecute(PublicBoardJson obj)
        {
            var selectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "Select folder to save public board",
                ShowNewFolderButton = true,
                SelectedPath = selectedPath
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = dialog.SelectedPath + "\\" + obj.Name + ".kam";
                HttpResponseMessage resp;

                HttpClient hc = new HttpClient();
                try
                {
                    resp = await hc.GetAsync(obj.Url);
                }
                catch(Exception ex)
                {
                    mon.ApplicationError($"StartupViewModel.OpenPublicBoardCommandExecute network error: {ex.Message}");
                    await dialCoord.ShowMessageAsync(this, "Network Error", "Can not download file");
                    return;
                }

                try
                {
                    await ReadAsFileAsync(resp.Content, fileName, true);
                }
                catch(Exception ex)
                {
                    mon.ApplicationError($"StartupViewModel.OpenPublicBoardCommandExecute file save error: {ex.Message}");
                    await dialCoord.ShowMessageAsync(this, "Error", "Can not save file");
                    return;
                }

                if (await OpenBoardView(fileName))
                    appConfig.UpdateRecent(fileName, false);
            }
        }

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
            if (initialized)
                return;

            shell.AddGlobalCommand("File", "Create", "NewFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "Open", "OpenFileCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Export", "ExportCommand", this)
                .SetHotKey(ModifierKeys.Control, Key.U);

            shell.AddGlobalCommand("File", "Show Startup", "ShowStartupCommand", this, true);

            shell.AddGlobalCommand("File", "Exit", "ExitCommand", this);

            initialized = true;

            Observable.FromAsync(_ => appConfig.LoadOnlineContentAsync())
                .Subscribe(_ =>
                {
                    PublicBoards = appConfig.PublicBoards;
                });
        }

        public async Task ReadAsFileAsync(HttpContent content, string filename, bool overwrite)
        {
            string pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                await content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        fileStream.Close();
                    });
            }
            catch
            {
                if (fileStream != null)
                    fileStream.Close();

                throw;
            }
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

        public static bool IsYesterday(this DateTime dt)
        {
            var yesterday = DateTime.Now - TimeSpan.FromDays(1);

            return dt.Year == yesterday.Year
                && dt.Month == yesterday.Month
                && dt.Day == yesterday.Day;
        }

        public static bool IsThisMonth(this DateTime datetime1)
        {
            return IsSameMonth(datetime1, DateTime.Now);
        }
    }
}
