using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ui.Wpf.Common;
using Autofac;
using DynamicData;

namespace Kamban.Model
{
    public interface IAppModel
    {
        SourceList<DbViewModel> RecentsDb { get; }

        void Initialize();
        DbViewModel AddRecent(string uri);
        void RemoveRecent(string uri);

        string Caption { get; set; }
        string ArchiveFolder { get; set; }

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
            ArchiveFolder = "";
        }
    }

    public class AppModel : IAppModel
    {
        private readonly IShell shell;
        public SourceList<DbViewModel> RecentsDb { get; private set; }

        public AppModel(IShell shell)
        {
            this.shell = shell;
            RecentsDb = new SourceList<DbViewModel>();

            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path += "\\kanban.config";
        }

        public void Initialize()
        {
            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                appConfig = JsonConvert.DeserializeObject<AppConfig>(data);
            }
            else
                appConfig = new AppConfig();

            RecentsDb.AddRange(appConfig.Recent.Select(x => new DbViewModel { Uri = x }));
        }

        private AppConfig appConfig;
        private readonly string path;

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

        // Obsolete
        public DbViewModel AddRecent(string uri)
        {
            var db = RecentsDb.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (db == null)
            {
                db = new DbViewModel { Uri = uri };
                RecentsDb.Add(db);

                appConfig.Recent.Insert(0, uri);
                appConfig.Recent = appConfig.Recent.Distinct().ToList();
                SaveConfig();
            }

            db.LastAccess = DateTime.Now;

            return db;
        }

        public void RemoveRecent(string uri)
        {
            appConfig.Recent.Remove(uri);
            SaveConfig();

            var db = RecentsDb.Items.Where(x => x.Uri == uri).FirstOrDefault();
            if (db != null)
                RecentsDb.Remove(db);
        }

        private void SaveConfig()
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
