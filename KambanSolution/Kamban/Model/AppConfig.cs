using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.Model
{
    public interface IAppConfig
    {
        string[] Recent { get; }
        string Caption { get; set; }
        string ArchiveFolder { get; set; }

        void AddRecent(string uri);
        void RemoveRecent(string uri);
    }

    public class AppConfigJson
    {
        public List<string> Recent { get; set; } = new List<string>();
        public string Caption { get; set; } = "KAMBAN";
        public string ArchiveFolder { get; set; } = null;
    }

    public class AppConfig : IAppConfig
    {
        private readonly AppConfigJson appConfig;
        private readonly string path;

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
        }

        public string[] Recent => appConfig.Recent.ToArray();

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

        public void AddRecent(string uri)
        {
            if (!appConfig.Recent.Contains(uri))
            {
                appConfig.Recent.Add(uri);
                SaveConfig();
            }
        }

        public void RemoveRecent(string uri)
        {
            if (appConfig.Recent.Contains(uri))
            {
                appConfig.Recent.Remove(uri);
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string data = JsonConvert.SerializeObject(appConfig);
            File.WriteAllText(path, data);
        }

    }//end of class
}
