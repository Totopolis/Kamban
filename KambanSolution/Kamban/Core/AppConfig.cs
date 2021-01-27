using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using Kamban.ViewModels;
using Kamban.ViewModels.Core;
using Kamban.Views;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using wpf.ui;

namespace Kamban.Core
{
    public partial class AppConfig : ReactiveObject, IAppConfig
    {
        private readonly IShell shell;
        private readonly ILogger log;
        private readonly AppConfigJson appConfigJson;
        private readonly string appConfigPath;
        private readonly SourceList<RecentViewModel> recentList;
        private readonly SourceList<PublicBoardJson> publicBoards;

        public string ServerName { get; } = "https://raw.githubusercontent.com/Totopolis/Kamban.Public/master/";

        // C:\Users\myuser\AppData\Roaming (travel with user profile)
        public static string GetRomaingPath(string fileName) =>
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Kamban\{fileName}";

        [Reactive] private string GetStartedValue { get; set; }
        [Reactive] private string BasementValue { get; set; }

        public AppConfig(IShell sh, ILogger l)
        {
            shell = sh;
            log = l;

            appConfigPath = GetRomaingPath("kamban.config");
            FileInfo file = new FileInfo(appConfigPath);

            if (file.Exists)
            {
                string data = File.ReadAllText(appConfigPath);

                try
                {
                    appConfigJson = JsonConvert.DeserializeObject<AppConfigJson>(data);
                }
                catch { }
            }

            // file not exists or deserialize error
            if (appConfigJson == null)
                appConfigJson = new AppConfigJson();

            if (string.IsNullOrEmpty(appConfigJson.AppGuid))
                appConfigJson.AppGuid = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(appConfigJson.ColorTheme))
                appConfigJson.ColorTheme = Color.FromArgb(255, 255, 255, 255).ToString();

            OpenLatestAtStartupValue = appConfigJson.OpenLatestAtStartup;
            ShowFileNameInTabValue = appConfigJson.ShowFileNameInTab;
            ColorThemeValue = ToColor(appConfigJson.ColorTheme);

            appConfigJson.StartNumber++;
            SaveConfig();

            recentList = new SourceList<RecentViewModel>();
            recentList.AddRange(appConfigJson.Feed.Select(x => new RecentViewModel
            { Uri = x.Uri, LastAccess = x.LastAccess, Pinned = x.Pinned }));

            RecentObservable = recentList.Connect().AutoRefresh();

            publicBoards = new SourceList<PublicBoardJson>();
            publicBoards
                .Connect()
                .Bind(out ReadOnlyObservableCollection<PublicBoardJson> temp)
                .Subscribe();

            PublicBoards = temp;

            GetStarted = this.WhenAnyValue(x => x.GetStartedValue);
            Basement = this.WhenAnyValue(x => x.BasementValue);
            OpenLatestAtStartupObservable = this.WhenAnyValue(x => x.OpenLatestAtStartupValue);
            ShowFileNameInTabObservable = this.WhenAnyValue(x => x.ShowFileNameInTabValue);
            ColorThemeObservable = this.WhenAnyValue(x => x.ColorThemeValue);
            // Manage current opened boards for raise on next startup

            shell.DockingManager.ActiveContentChanged += (s, e) =>
            {
                var view = shell.DockingManager.ActiveContent as BoardView;
                if (view == null)
                    return;
                var vm = view.ViewModel as BoardEditViewModel;
                if (vm.Box == null)
                    return;
                if (!appConfigJson.LatestOpenedAtStartup.Contains(vm.Box.Uri))
                    appConfigJson.LatestOpenedAtStartup.Add(vm.Box.Uri);
                SaveConfig();
            };

            shell.DockingManager.DocumentClosed += (s, e) =>
            {
                var view = e.Document.Content as BoardView;
                if (view == null)
                    return;

                var vm = view.ViewModel as BoardEditViewModel;
                appConfigJson.LatestOpenedAtStartup.Remove(vm.Box.Uri);
                SaveConfig();
            };
        }

        private Color ToColor(string str)
        {
            TypeConverter cc = TypeDescriptor.GetConverter(typeof(Color));
            var result = (Color)cc.ConvertFromString(str);
            return result;
        }

        public IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; private set; }

        public ReadOnlyObservableCollection<PublicBoardJson> PublicBoards { get; private set; }

        public IObservable<string> GetStarted { get; private set; }

        public IObservable<string> Basement { get; private set; }

        public string Caption
        {
            get => appConfigJson.Caption;
            set
            {
                appConfigJson.Caption = value;
                SaveConfig();
            }
        }

        public string ArchiveFolder
        {
            get => appConfigJson.ArchiveFolder;
            set
            {
                appConfigJson.ArchiveFolder = value;
                SaveConfig();
            }
        }

        public string LastRedmineUrl
        {
            get => appConfigJson.LastRedmineUrl;
            set
            {
                appConfigJson.LastRedmineUrl = value;
                SaveConfig();
            }
        }

        public string LastRedmineUser
        {
            get => appConfigJson.LastRedmineUser;
            set
            {
                appConfigJson.LastRedmineUser = value;
                SaveConfig();
            }
        }

        public string AppGuid => appConfigJson.AppGuid;

        public IEnumerable<string> LastOpenedAtStartup => appConfigJson.LatestOpenedAtStartup;

        public void UpdateRecent(string uri, bool pinned)
        {
            var now = DateTime.Now;

            var recentVm = recentList.Items.FirstOrDefault(x => x.Uri == uri);
            if (recentVm == null)
            {
                recentVm = new RecentViewModel
                {
                    Uri = uri,
                    LastAccess = now
                };

                recentList.Add(recentVm);
            }
            else
            {
                recentVm.LastAccess = now;
                recentVm.Pinned = pinned;
            }

            var recentJson = appConfigJson.Feed.FirstOrDefault(x => x.Uri == uri);
            if (recentJson == null)
            {
                recentJson = new RecentJson
                {
                    Uri = uri,
                    LastAccess = now
                };

                appConfigJson.Feed.Add(recentJson);
            }
            else
            {
                recentJson.LastAccess = now;
                recentJson.Pinned = pinned;
            }

            SaveConfig();
        }

        public void RemoveRecent(string uri)
        {
            var recentVm = recentList.Items.FirstOrDefault(x => x.Uri == uri);
            var recentJson = appConfigJson.Feed.FirstOrDefault(x => x.Uri == uri);

            if (recentVm == null || recentJson == null)
                return;

            recentList.Remove(recentVm);
            appConfigJson.Feed.Remove(recentJson);
            SaveConfig();
        }

        private void SaveConfig()
        {
            string data = JsonConvert.SerializeObject(appConfigJson);
            File.WriteAllText(appConfigPath, data);
        }

        public async Task LoadOnlineContentAsync()
        {
            try
            {
                var common = await DownloadAndDeserialize<CommonJson>("common.json");

                var startup = await DownloadAndDeserialize<StartupConfigJson>($"{appConfigJson.Language}/startup.json");
                appConfigJson.Startup = startup;
                appConfigJson.Common = common;

                SaveConfig();
            }
            catch (Exception ex)
            {
                log.Error($"AppConfig.LoadOnlineContentAsync download error: {ex.Message}");
            }

            if (appConfigJson.Startup != null)
            {
                publicBoards.AddRange(appConfigJson.Startup.PublicBoards);

                BasementValue = appConfigJson.Startup.Basement;

                int tipsCount = appConfigJson.Startup.Tips.Count;

                if (tipsCount == 0 || appConfigJson.StartNumber == 1)
                {
                    GetStartedValue = appConfigJson.Startup.FirstStart;
                    return;
                }

                // tips rotate
                int indx = appConfigJson.StartNumber - 2;
                // preserve of out-of-range
                if (indx >= tipsCount)
                    indx = 1;

                GetStartedValue = appConfigJson.Startup.Tips[indx];

                if (indx == tipsCount - 1)
                {
                    appConfigJson.StartNumber = 1;
                    SaveConfig();
                }
            }
        } //LoadOnlineContentAsync

        private async Task<T> DownloadAndDeserialize<T>(string path)
            where T : class
        {
            var hc = new HttpClient();
            var resp = await hc.GetAsync(ServerName + path);
            var str = await resp.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(str);
        }
    } //end of class
}
