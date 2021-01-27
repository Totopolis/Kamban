using System;
using System.Windows.Media;
using ReactiveUI.Fody.Helpers;

namespace Kamban.Core
{
    // SettingsView items
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
                    appConfigJson.OpenLatestAtStartup = value;
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
                    appConfigJson.ShowFileNameInTab = value;
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
                    appConfigJson.ColorTheme = value.ToString();
                    SaveConfig();

                    ColorThemeValue = value;
                }
            }
        }
    }//end of class
}
