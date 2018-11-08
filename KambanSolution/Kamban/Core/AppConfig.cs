using DynamicData;
using Kamban.Model;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Core
{
    public interface IAppConfig
    {
        string Caption { get; set; }
        string ArchiveFolder { get; set; }

        void UpdateRecent(string uri, bool pinned);
        void RemoveRecent(string uri);

        IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; }
    }

    public class RecentJson
    {
        public string Uri { get; set; }
        public DateTime LastAccess { get; set; }
        public bool Pinned { get; set; }
    }

    public class AppConfigJson
    {
        public List<RecentJson> Feed { get; set; } = new List<RecentJson>();
        public string Caption { get; set; } = "KAMBAN";
        public string ArchiveFolder { get; set; } = null;
    }

    public class AppConfig : IAppConfig
    {
        private readonly AppConfigJson appConfig;
        private readonly string path;
        private readonly SourceList<RecentViewModel> recentList;

        public AppConfig()
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path += "\\kanban.config";

            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                appConfig = JsonConvert.DeserializeObject<AppConfigJson>(data);
            }
            else
                appConfig = new AppConfigJson();

            recentList = new SourceList<RecentViewModel>();
            recentList.AddRange(appConfig.Feed.Select(x => new RecentViewModel
                { Uri = x.Uri, LastAccess = x.LastAccess, Pinned = x.Pinned }));

            RecentObservable = recentList.Connect().AutoRefresh();
        }

        public IObservable<IChangeSet<RecentViewModel>> RecentObservable { get; private set; }

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

    }//end of class
}
