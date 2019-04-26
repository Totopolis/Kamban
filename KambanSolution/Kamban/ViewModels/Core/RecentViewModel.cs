using System;
using System.IO;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Kamban.ViewModels.Core
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
