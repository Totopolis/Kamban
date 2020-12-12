using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Text;
using ReactiveUI.Fody.Helpers;

namespace Kamban.Core
{
    public partial class AppConfig
    {
        [Reactive] private bool OpenLatestAtStartupValue { get; set; }
        [Reactive] private bool ShowFileNameInTabValue { get; set; }
        [Reactive] private Color ColorThemeValue { get; set; }
        public IObservable<bool> OpenLatestAtStartupObservable { get; private set; }
        public IObservable<bool> ShowFileNameInTabObservable { get; private set; }
        public IObservable<Color> ColorThemeObservable { get; private set; }
        public bool OpenLatestAtStartup
        {
            get { return OpenLatestAtStartupValue; }
            set 
            {
                if (OpenLatestAtStartupValue != value)
                {
                    appConfig.OpenLatestAtStartup = value;
                    SaveConfig();

                    OpenLatestAtStartupValue = value;
                }
            }
        }

        public bool ShowFileNameInTab
        {
            get { return ShowFileNameInTabValue; }
            set 
            { 
                if (ShowFileNameInTabValue != value)
                {
                    appConfig.ShowFileNameInTab = value;
                    SaveConfig();

                    ShowFileNameInTabValue = value;
                }
            }
        }
        public Color ColorTheme
        {
            get { return ColorThemeValue; }
            set
            {
                if (ColorThemeValue != value)
                {
                    appConfig.ColorTheme = value;
                    SaveConfig();

                    ColorThemeValue = value;
                }
            }
        }
    }
}
