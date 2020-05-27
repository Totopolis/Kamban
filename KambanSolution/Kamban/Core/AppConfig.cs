using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DynamicData;
using Kamban.ViewModels.Core;
using Monik.Common;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.Core
{
    public class AppConfig : ReactiveObject, IAppConfig
    {
        private readonly IMonik mon;
        private readonly AppConfigJson appConfig;
        private readonly string appConfigPath;
        private readonly SourceList<RecentViewModel> recentList;
        private readonly SourceList<PublicBoardJson> publicBoards;

        public string ServerName { get; } = "https://raw.githubusercontent.com/Totopolis/Kamban.Public/master/";

        // C:\Users\myuser\AppData\Roaming (travel with user profile)
        public static string GetRomaingPath(string fileName) =>
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Kamban\{fileName}";

        [Reactive] private string GetStartedValue { get; set; }
        [Reactive] private string BasementValue { get; set; }

        public AppConfig(IMonik m)
        {
            mon = m;

            appConfigPath = GetRomaingPath("kamban.config");
            FileInfo file = new FileInfo(appConfigPath);
            file.Directory.Create();

            if (file.Exists)
            {
                string data = File.ReadAllText(appConfigPath);
                appConfig = JsonConvert.DeserializeObject<AppConfigJson>(data);
            }
            else
                appConfig = new AppConfigJson();

            appConfig.StartNumber++;
            SaveConfig();

            recentList = new SourceList<RecentViewModel>();
            recentList.AddRange(appConfig.Feed.Select(x => new RecentViewModel
                {Uri = x.Uri, LastAccess = x.LastAccess, Pinned = x.Pinned}));

            RecentObservable = recentList.Connect().AutoRefresh();

            publicBoards = new SourceList<PublicBoardJson>();
            publicBoards
                .Connect()
                .Bind(out ReadOnlyObservableCollection<PublicBoardJson> temp)
                .Subscribe();

            PublicBoards = temp;

            GetStarted = this.WhenAnyValue(x => x.GetStartedValue);
            Basement = this.WhenAnyValue(x => x.BasementValue);
        }

        public IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; private set; }

        public ReadOnlyObservableCollection<PublicBoardJson> PublicBoards { get; private set; }

        public IObservable<string> GetStarted { get; private set; }

        public IObservable<string> Basement { get; private set; }

        public string Caption
        {
            get => appConfig.Caption;
            set
            {
                appConfig.Caption = value;
                SaveConfig();
            }
        }

        public string ArchiveFolder
        {
            get => appConfig.ArchiveFolder;
            set
            {
                appConfig.ArchiveFolder = value;
                SaveConfig();
            }
        }

        public string LastRedmineUrl
        {
            get => appConfig.LastRedmineUrl;
            set
            {
                appConfig.LastRedmineUrl = value;
                SaveConfig();
            }
        }

        public string LastRedmineUser
        {
            get => appConfig.LastRedmineUser;
            set
            {
                appConfig.LastRedmineUser = value;
                SaveConfig();
            }
        }

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

            var recentJson = appConfig.Feed.FirstOrDefault(x => x.Uri == uri);
            if (recentJson == null)
            {
                recentJson = new RecentJson
                {
                    Uri = uri,
                    LastAccess = now
                };

                appConfig.Feed.Add(recentJson);
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
            var recentJson = appConfig.Feed.FirstOrDefault(x => x.Uri == uri);

            if (recentVm == null || recentJson == null)
                return;

            recentList.Remove(recentVm);
            appConfig.Feed.Remove(recentJson);
            SaveConfig();
        }

        private void SaveConfig()
        {
            string data = JsonConvert.SerializeObject(appConfig);
            File.WriteAllText(appConfigPath, data);
        }

        public async Task LoadOnlineContentAsync()
        {
            try
            {
                var common = await DownloadAndDeserialize<CommonJson>("common.json");

                var startup = await DownloadAndDeserialize<StartupConfigJson>($"{appConfig.Language}/startup.json");
                appConfig.Startup = startup;
                appConfig.Common = common;

                SaveConfig();
            }
            catch (Exception ex)
            {
                mon.ApplicationError($"AppConfig.LoadOnlineContentAsync download error: {ex.Message}");
            }

            if (appConfig.Startup != null)
            {
                publicBoards.AddRange(appConfig.Startup.PublicBoards);

                BasementValue = appConfig.Startup.Basement;

                int tipCount = appConfig.Startup.Tips.Count;

                // TODO: fix rotator

                if (tipCount == 0 || appConfig.StartNumber == 1)
                {
                    GetStartedValue = appConfig.Startup.FirstStart;
                    return;
                }

                // tips rotate
                int indx = appConfig.StartNumber - 2;
                GetStartedValue = appConfig.Startup.Tips[indx];

                if (indx == appConfig.Startup.Tips.Count - 1)
                {
                    appConfig.StartNumber = 1;
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
