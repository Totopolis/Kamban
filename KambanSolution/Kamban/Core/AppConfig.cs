using DynamicData;
using Kamban.Model;
using Monik.Common;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kamban.Core
{
    public interface IAppConfig
    {
        string ServerName { get; }
        string Caption { get; set; }
        string ArchiveFolder { get; set; }

        void UpdateRecent(string uri, bool pinned);
        void RemoveRecent(string uri);

        IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; }
        ReadOnlyObservableCollection<PublicBoardJson> PublicBoards { get; }
        IObservable<string> GetStarted { get; }
        IObservable<string> Basement { get; }

        Task LoadOnlineContentAsync();
    }

    public class AppConfig : ReactiveObject, IAppConfig
    {
        private readonly IMonik mon;
        private readonly AppConfigJson appConfig;
        private readonly string path;
        private readonly SourceList<RecentViewModel> recentList;
        private readonly SourceList<PublicBoardJson> publicBoards;

        public string ServerName { get; } = "http://topols.io/kamban/";

        [Reactive] private string getStartedValue { get; set; }
        [Reactive] private string basementValue { get; set; }

        public AppConfig(IMonik m)
        {
            mon = m;

            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path += "\\kanban.config";

            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                appConfig = JsonConvert.DeserializeObject<AppConfigJson>(data);
            }
            else
                appConfig = new AppConfigJson();

            appConfig.StartNumber++;
            SaveConfig();

            recentList = new SourceList<RecentViewModel>();
            recentList.AddRange(appConfig.Feed.Select(x => new RecentViewModel
                { Uri = x.Uri, LastAccess = x.LastAccess, Pinned = x.Pinned }));

            RecentObservable = recentList.Connect().AutoRefresh();

            publicBoards = new SourceList<PublicBoardJson>();
            publicBoards
                .Connect()
                .Bind(out ReadOnlyObservableCollection<PublicBoardJson> temp)
                .Subscribe();

            PublicBoards = temp;

            GetStarted = this.WhenAnyValue(x => x.getStartedValue);
            Basement = this.WhenAnyValue(x => x.basementValue);
        }

        public IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; private set; }

        public ReadOnlyObservableCollection<PublicBoardJson> PublicBoards { get; private set; }

        public IObservable<string> GetStarted { get; private set; }

        public IObservable<string> Basement { get; private set; }

        public string Caption
        {
            get { return appConfig.Caption; }
            set
            {
                appConfig.Caption = value;
                SaveConfig();
            }
        }

        public string ArchiveFolder
        {
            get { return appConfig.ArchiveFolder; }
            set
            {
                appConfig.ArchiveFolder = value;
                SaveConfig();
            }
        }

        public void UpdateRecent(string uri, bool pinned)
        {
            var recentVM = recentList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            var recentJson = appConfig.Feed.Where(x => x.Uri == uri).FirstOrDefault();

            if (recentVM == null)
            {
                recentVM = new RecentViewModel
                {
                    Uri = uri,
                    LastAccess = DateTime.Now
                };

                recentList.Add(recentVM);

                recentJson = new RecentJson
                {
                    Uri = uri,
                    LastAccess = DateTime.Now
                };

                appConfig.Feed.Add(recentJson);
                SaveConfig();
            }
            else
            {
                recentVM.LastAccess = DateTime.Now;
                recentJson.LastAccess = recentVM.LastAccess;

                recentVM.Pinned = pinned;
                recentJson.Pinned = pinned;

                SaveConfig();
            }
        }

        public void RemoveRecent(string uri)
        {
            var recentVM = recentList.Items.Where(x => x.Uri == uri).FirstOrDefault();
            var recentJson = appConfig.Feed.Where(x => x.Uri == uri).FirstOrDefault();

            if (recentVM == null)
                return;

            recentList.Remove(recentVM);
            appConfig.Feed.Remove(recentJson);
            SaveConfig();
        }

        private void SaveConfig()
        {
            string data = JsonConvert.SerializeObject(appConfig);
            File.WriteAllText(path, data);
        }

        public async Task LoadOnlineContentAsync()
        {
            try
            {
                var ver = await DownloadAndDeserialize<VersionJson>("ver.json");
                if (ver.StartUpVersion > appConfig.Version.StartUpVersion)
                {
                    var startup = await DownloadAndDeserialize<StartupConfigJson>("startup.json");
                    appConfig.Startup = startup;
                    appConfig.Version = ver;

                    SaveConfig();
                }
            }
            catch(Exception ex)
            {
                mon.ApplicationError($"AppConfig.LoadOnlineContentAsync download error: {ex.Message}");
            }

            if (appConfig.Startup != null)
            {
                publicBoards.AddRange(appConfig.Startup.PublicBoards);

                basementValue = appConfig.Startup.Basement;

                int tipCount = appConfig.Startup.Tips.Count;

                if (tipCount == 0 || appConfig.StartNumber == 1)
                {
                    getStartedValue = appConfig.Startup.FirstStart;
                    return;
                }

                // tips rotate
                int indx = appConfig.StartNumber - 2;
                getStartedValue = appConfig.Startup.Tips[indx];

                if (indx == appConfig.Startup.Tips.Count - 1)
                {
                    appConfig.StartNumber = 1;
                    SaveConfig();
                }
            }
        }

        private async Task<T> DownloadAndDeserialize<T>(string path)
            where T: class
        {
            HttpClient hc = new HttpClient();
            var resp = await hc.GetAsync(ServerName + path);
            var str = await resp.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(str);
        }

    }//end of class
}
