using System;
using System.Windows.Media;
using Autofac.Core;
using Kamban.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using wpf.ui;

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

        [Reactive] public Color ColorTheme { get; set; }
        private IDisposable ColorThemeDispose;
        public SettingsViewModel(IShell sh, IAppConfig ac)
        {
            shell = sh;
            appConfig = ac;

            OpenLatestAtStartup = appConfig.OpenLatestAtStartup;
            ShowFileNameInTab = appConfig.ShowFileNameInTab;
            ColorTheme = appConfig.ColorTheme;

            OpenLatestAtStartupDispose = this.WhenAnyValue(x => x.OpenLatestAtStartup)
                .Subscribe(x => appConfig.OpenLatestAtStartup = x);

            ShowFileNameInTabDispose = this.WhenAnyValue(x => x.ShowFileNameInTab)
                .Subscribe(x => appConfig.ShowFileNameInTab = x);

            ColorThemeDispose = this.WhenAnyValue(x => x.ColorTheme)
                .Subscribe(x => appConfig.ColorTheme = x);
        }
    }//end of vm
}
