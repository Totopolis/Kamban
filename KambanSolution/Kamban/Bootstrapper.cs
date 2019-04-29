using Autofac;
using AutoMapper;
using Kamban.Core;
using Kamban.Export;
using Kamban.Templates;
using Kamban.ViewModels;
using Kamban.ViewModels.Core;
using Kamban.Views;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban
{
    public class Bootstrapper : IBootstraper
    {
        public IShell Init()
        {
            var container = ConfigureContainer();
            var shell = container.Resolve<IShell>();
            shell.Container = container;

            var mon = container.Resolve<IMonik>();
            mon.ApplicationInfo("Bootstrapper initialized");

            return shell;
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ShellEx>()
                .As<IShell>()
                .SingleInstance();

            builder
                .RegisterType<MainWindow>()
                .As<IDockWindow>();

            builder
                .RegisterType<ExportService>()
                .As<IExportService>();

            builder
                .RegisterType<StaticTemplates>()
                .As<ITemplates>()
                .SingleInstance();

            builder
                .RegisterType<AppConfig>()
                .As<IAppConfig>()
                .SingleInstance();

            builder
                .RegisterType<AppModel>()
                .As<IAppModel>()
                .SingleInstance();

            builder.RegisterInstance(DialogCoordinator.Instance)
                .As<IDialogCoordinator>()
                .SingleInstance();

            builder.RegisterInstance(new MonikFile("kamban.log"))
                .As<IMonik>()
                .SingleInstance();

            builder.RegisterInstance(
                    new MapperConfiguration(cfg => { cfg.AddProfile<MapperProfile>(); })
                        .CreateMapper())
                .As<IMapper>()
                .SingleInstance();

            builder.RegisterType<BoxViewModel>();

            ConfigureSingleView<StartupViewModel, StartupView>(builder);
            ConfigureView<WizardViewModel, WizardView>(builder);
            ConfigureView<BoardEditViewModel, BoardView>(builder);
            ConfigureView<BoardEditForExportViewModel, BoardEditForExportView>(builder);
            ConfigureView<ExportViewModel, ExportView>(builder);

            return builder.Build();
        }

        private static void ConfigureView<TViewModel, TView>(ContainerBuilder builder)
            where TViewModel : IViewModel
            where TView : IView
        {
            builder.RegisterType<TViewModel>();
            builder.RegisterType<TView>();
        }

        private static void ConfigureSingleView<TViewModel, TView>(ContainerBuilder builder)
            where TViewModel : IViewModel
            where TView : IView
        {
            builder.RegisterType<TViewModel>()
                .SingleInstance();

            builder.RegisterType<TView>()
                .SingleInstance();
        }
    }
}