using Kamban.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ShowOptions;

namespace Kamban
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var shell = UiStarter.Start<IDockWindow>(
                new Bootstrapper(),
                new UiShowStartWindowOptions
                {
                    Title = "Kamban",
                    ToolPaneWidth = 100
                });

            // TODO: used hack, need fix WPFToolkit
            shell.Title = "Kamban";

            shell.ShowView<StartupView>(options: new UiShowOptions() { Title = "Start Page", CanClose = false });
        }
    }
}
