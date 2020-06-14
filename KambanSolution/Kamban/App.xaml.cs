using System.Windows;
using Autofac;
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

            shell.DockingManager.ActiveContentChanged += (s, e) =>
            {
                var ga = shell.Container.Resolve<IGa>();
                var view = shell.SelectedView;
                if (view == null)
                    return;

                if (view is StartupView)
                    ga.TrackPage("startup");
                else if (view is SettingsView)
                    ga.TrackPage("settings");
                else if (view is BoardView)
                    ga.TrackPage("board");
                else if (view is ExportView)
                    ga.TrackPage("export");
                else if (view is ImportView)
                    ga.TrackPage("import");
                else if (view is WizardView)
                    ga.TrackPage("create");
            };

            shell.ShowView<StartupView>(
                viewRequest: new ViewRequest { ViewId = StartupViewModel.StartupViewId },
                options: new UiShowOptions { Title = "Start Page", CanClose = false });
        }
    }
}
