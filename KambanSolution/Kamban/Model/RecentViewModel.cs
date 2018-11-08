using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;

namespace Kamban.Model
{
    public class RecentViewModel : ReactiveObject
    {
        [Reactive] public string Uri { get; set; }
        [Reactive] public DateTime LastAccess { get; set; }
        [Reactive] public bool Pinned { get; set; }

        public string FileName => (new FileInfo(Uri)).Name;
        public string PathName => (new FileInfo(Uri)).DirectoryName;
    }
}
