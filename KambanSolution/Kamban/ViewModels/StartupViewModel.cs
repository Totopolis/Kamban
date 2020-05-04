using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using AutoMapper;
using DynamicData;
using Kamban.Core;
using Kamban.ViewModels.Core;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
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
        private readonly IMonik mon;
        private bool initialized;

        public const string StartupViewId = "KambanStartupView";

        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Pinned { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Today { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Yesterday { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> ThisMonth { get; private set; }
        [Reactive] public IObservable<IChangeSet<RecentViewModel>> Older { get; private set; }

        public ReactiveCommand<Unit, Unit> NewFileCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; private set; }
        [Reactive] public ReactiveCommand<RecentViewModel, Unit> OpenRecentBoxCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ImportCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> PrintCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ShowStartupCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; private set; }

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

            OpenRecentBoxCommand = ReactiveCommand.Create<RecentViewModel>(async (rvm) =>
            {
                if (await OpenBoardView(rvm.Uri))
                    appConfig.UpdateRecent(rvm.Uri, rvm.Pinned);
            });

            NewFileCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<WizardView>(
                    new WizardViewRequest { ViewId = "Creating new file", Uri = null }));

            OpenFileCommand = ReactiveCommand.CreateFromTask(async _ =>
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

            ImportCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<ImportView>(null, new UiShowOptions { Title = "Import" }));

            var whenBoardSelected = shell
                .WhenAny(x => x.SelectedView, x => x.Value?.ViewModel is BoardEditViewModel)
                .Publish();

            ExportCommand = ReactiveCommand.Create(() =>
                this.shell.ShowView<ExportView>(null, new UiShowOptions { Title = "Export" }),
                whenBoardSelected);

            PrintCommand = ReactiveCommand.Create(PrintCommandExecute, whenBoardSelected);

            whenBoardSelected.Connect();

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
                .Subscribe(x => Basement = x + $"v{ver.Version.Major}.{ver.Version.Minor}.{ver.Version.Build}");
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
                appModel.Remove(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was deleted or moved");
                return false;
            }

            var box = await appModel.Load(uri);
            if (!box.Loaded)
            {
                appConfig.RemoveRecent(uri);
                appModel.Remove(uri);

                await dialCoord.ShowMessageAsync(this, "Error", "File was damaged");
                return false;
            }

            var title = file.FullName;

            shell.ShowView<BoardView>(
                viewRequest: new BoardViewRequest { ViewId = title, Box = box },
                options: new UiShowOptions { Title = title });

            return true;
        }

        public void Initialize(ViewRequest viewRequest)
        {
            if (initialized)
                return;

            shell.AddGlobalCommand("File", "Create", nameof(NewFileCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.N);

            shell.AddGlobalCommand("File", "Open", nameof(OpenFileCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.O);

            shell.AddGlobalCommand("File", "Import", nameof(ImportCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.I);

            shell.AddGlobalCommand("File", "Export", nameof(ExportCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.U);

            shell.AddGlobalCommand("File", "Print", nameof(PrintCommand), this)
                .SetHotKey(ModifierKeys.Control, Key.P);

            shell.AddGlobalCommand("File", "Show Startup", nameof(ShowStartupCommand), this, true);

            shell.AddGlobalCommand("File", "Exit", nameof(ExitCommand), this);

            initialized = true;

            Observable.FromAsync(_ => appConfig.LoadOnlineContentAsync())
                .Subscribe(_ =>
                {
                    PublicBoards = appConfig.PublicBoards;
                });
        }

        public async Task ReadAsFileAsync(HttpContent content, string filename, bool overwrite)
        {
            var pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
                throw new InvalidOperationException($"File {pathname} already exists.");

            using (Stream fs = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None))
            { 
                await content.CopyToAsync(fs);
            }
        }

        private void PrintCommandExecute()
        {
            if (shell.SelectedView.ViewModel is BoardEditViewModel bvm)
            {
                ((ShellEx)shell).PrintView<BoardEditForExportView>(
                    bvm.Box.Boards.Items.Select(x =>
                            new BoardViewRequest
                            {
                                ViewId = bvm.Box.Uri,
                                ShowCardIds = bvm.ShowCardIds,
                                SwimLaneView = bvm.SwimLaneView,
                                Box = bvm.Box,
                                Board = x
                            })
                        .Cast<ViewRequest>()
                        .ToArray()
                );
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
