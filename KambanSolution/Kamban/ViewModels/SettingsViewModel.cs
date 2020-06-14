using System;
using Autofac.Core;
using Kamban.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private IShell shell;
        private readonly IAppConfig appConfig;

        public const string StartupViewId = "SettingsView";

        [Reactive] public bool OpenLatestAtStartup { get; set; }
        private IDisposable OpenLatestAtStartupDispose;

        [Reactive] public bool ShowFileNameInTab { get; set; }
        private IDisposable ShowFileNameInTabDispose;

        public SettingsViewModel(IShell sh, IAppConfig ac)
        {
            shell = sh;
            appConfig = ac;

            OpenLatestAtStartup = appConfig.OpenLatestAtStartup;
            ShowFileNameInTab = appConfig.ShowFileNameInTab;

            OpenLatestAtStartupDispose = this.WhenAnyValue(x => x.OpenLatestAtStartup)
                .Subscribe(x => appConfig.OpenLatestAtStartup = x);

            ShowFileNameInTabDispose = this.WhenAnyValue(x => x.ShowFileNameInTab)
                .Subscribe(x => appConfig.ShowFileNameInTab = x);
        }
    }//end of vm
}
