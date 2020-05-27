using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using Kamban.ViewModels.Core;

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

        string LastRedmineUrl { get; set; }
        string LastRedmineUser { get; set; }
    }
}
