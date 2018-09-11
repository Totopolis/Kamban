using Autofac;
using Kamban.Models;
using Kamban.SqliteLocalStorage;
using Kamban.Views;
using Kamban.ViewModels;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using Kamban.Repository;

namespace Kamban
{
    public class Bootstrapper : IBootstraper
    {
        public IShell Init()
        {
            var container = ConfigureContainer();
            var shell = container.Resolve<IShell>();
            shell.Container = container;
            return shell;
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DistinctShell>()
                .As<IShell>()
                .SingleInstance();

            builder
                .RegisterType<MainWindow>()
                .As<IDockWindow>();

            builder
                .RegisterType<LiteDbRepository>()
                .As<IRepository>();

            builder
                .RegisterType<ScopeModel>()
                .As<IScopeModel>();

            builder
                .RegisterType<AppModel>()
                .As<IAppModel>()
                .SingleInstance();

            ConfigureView<StartupViewModel, StartupView>(builder);
            ConfigureView<WizardViewModel, WizardView>(builder);
            ConfigureView<BoardViewModel, BoardView>(builder);

            return builder.Build();
        }

        private static void ConfigureView<TViewModel, TView>(ContainerBuilder builder)
            where TViewModel : IViewModel
            where TView : IView
        {
            builder.RegisterType<TViewModel>();
            builder.RegisterType<TView>();
        }
    }
}
