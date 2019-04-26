using System.Windows;
using Kamban.ViewModels;
using Kamban.Views;
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
                    Title = "Kamban"
                });

            shell.ShowView<StartupView>(
                viewRequest: new ViewRequest { ViewId = StartupViewModel.StartupViewId },
                options: new UiShowOptions { Title = "Start Page", CanClose = false });
        }
    }
}
