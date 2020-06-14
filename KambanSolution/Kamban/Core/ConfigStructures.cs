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

    /// <summary>
    /// Downloaded from github
    /// </summary>
    public class StartupConfigJson
    {
        public string FirstStart { get; set; }
        public List<string> Tips { get; set; } = new List<string>();
        public string Basement { get; set; }
        public List<PublicBoardJson> PublicBoards { get; set; } = new List<PublicBoardJson>();
    }

    public class CommonJson
    {
        public List<string> Languages { get; set; }
        public string ProductionVersion { get; set; }
        public string DevelopVersion { get; set; }
    }

    /// <summary>
    /// Strored at app roaming folder
    /// </summary>
    public class AppConfigJson
    {
        public string Language { get; set; } = "en";

        public List<RecentJson> Feed { get; set; } = new List<RecentJson>();
        public string Caption { get; set; } = "KAMBAN";
        public string ArchiveFolder { get; set; } = null;

        public int StartNumber { get; set; } = 0;

        public CommonJson Common { get; set; } = new CommonJson();
        public StartupConfigJson Startup { get; set; } = null;

        public string LastRedmineUrl { get; set; } = "";
        public string LastRedmineUser { get; set; } = "";

        public string AppGuid { get; set; } = "";

        public bool OpenLatestAtStartup { get; set; }
        public bool ShowFileNameInTab { get; set; }
    }
}
