using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ui.Wpf.Common;
using Autofac;

namespace Kamban.Model
{
    public interface IAppModel
    {
        IEnumerable<string> GetRecentDocuments();
        void AddRecent(string doc);
        void RemoveRecent(string doc);

        string Caption { get; set; }
        string ArchiveFolder { get; set; }

        void LoadConfig();
        void SaveConfig();

        IProjectService CreateProjectService(string uri);
        IProjectService LoadProjectService(string uri);
    }

    public class AppConfig
    {
        public List<string> Recent { get; set; }
        public string Caption { get; set; }
        public string ArchiveFolder { get; set; }

        public AppConfig()
        {
            Recent = new List<string>();
            Caption = "";
        }
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;

        private AppConfig appConfig;
        private readonly string path;

        public string Caption
        {
            get => appConfig.Caption;
            set => appConfig.Caption = value;
        }

        public string ArchiveFolder
        {
            get => appConfig.ArchiveFolder;
            set => appConfig.ArchiveFolder = value;
        }

        public AppModel(IShell shell)
        {
            this.shell = shell;

            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); 
            path += "\\kanban.config";

            appConfig = new AppConfig();
        }

        public void AddRecent(string doc) 
        {
            appConfig.Recent.Insert(0, doc);
            appConfig.Recent = appConfig.Recent.Distinct().ToList();
        }

        public IEnumerable<string> GetRecentDocuments()
        {
            return appConfig.Recent;
        }

        public void RemoveRecent(string doc)
        {
            appConfig.Recent.Remove(doc);
        }

        public void LoadConfig()
        {
            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                appConfig = JsonConvert.DeserializeObject<AppConfig>(data);
            }
        }

        public void SaveConfig()
        {
            string data = JsonConvert.SerializeObject(appConfig);
            File.WriteAllText(path, data);
        }

        public IProjectService CreateProjectService(string uri)
        {
            var scope = shell
                .Container
                .Resolve<IProjectService>(new NamedParameter("uri", uri));

            return scope;
        }

        public IProjectService LoadProjectService(string uri)//future? taking from env too?
        {
            var scope = shell
                .Container
                .Resolve<IProjectService>(new NamedParameter("uri", uri));

            return scope;
        }
    }
}
