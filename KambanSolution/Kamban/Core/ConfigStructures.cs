using System;
using System.Collections.Generic;

namespace Kamban.Core
{
    public class RecentJson
    {
        public string Uri { get; set; }
        public DateTime LastAccess { get; set; }
        public bool Pinned { get; set; }
    }

    public class PublicBoardJson
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }

    public class StartupConfigJson
    {
        public string FirstStart { get; set; }
        public List<string> Tips { get; set; } = new List<string>();
        public string Basement { get; set; }
        public List<PublicBoardJson> PublicBoards { get; set; } = new List<PublicBoardJson>();
    }

    public class VersionJson
    {
        public int StartUpVersion { get; set; }
        public int TemplatesVersion { get; set; }
    }

    public class AppConfigJson
    {
        public List<RecentJson> Feed { get; set; } = new List<RecentJson>();
        public string Caption { get; set; } = "KAMBAN";
        public string ArchiveFolder { get; set; } = null;

        public int StartNumber { get; set; } = 0;

        public VersionJson Version { get; set; } = new VersionJson();
        public StartupConfigJson Startup { get; set; } = null;

        public string LastRedmineUrl { get; set; } = "";
        public string LastRedmineUser { get; set; } = "";
    }
}
