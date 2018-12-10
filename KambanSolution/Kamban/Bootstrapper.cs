using Autofac;
using AutoMapper;
using Kamban.Core;
using Kamban.Model;
using Kamban.Repository;
using Kamban.ViewModels;
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
                .RegisterType<LiteDbRepository>()
                .As<IRepository>();

            builder
                .RegisterType<ProjectService>()
                .As<IProjectService>();

            builder
                .RegisterType<ExportService>()
                .As<IExportService>();

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

            builder.RegisterInstance(ConfigureMapper().CreateMapper())
                .As<IMapper>()
                .SingleInstance();

            ConfigureSingleView<StartupViewModel, StartupView>(builder);
            ConfigureView<WizardViewModel, WizardView>(builder);
            ConfigureView<BoardEditViewModel, BoardView>(builder);
            ConfigureView<BoardEditForExportViewModel, BoardForExportView>(builder);
            ConfigureView<ExportViewModel, ExportView>(builder);

            return builder.Build();
        }

        private static MapperConfiguration ConfigureMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<BoardViewModel, BoardInfo>();
                cfg.CreateMap<BoardInfo, BoardViewModel>();

                cfg.CreateMap<CardViewModel, Issue>()
                    .ForMember(dst => dst.Head, opt => opt.MapFrom(src => src.Header))
                    .ForMember(dst => dst.ColumnId, opt => opt.MapFrom(src => src.ColumnDeterminant))
                    .ForMember(dst => dst.RowId, opt => opt.MapFrom(src => src.RowDeterminant));

                cfg.CreateMap<Issue, CardViewModel>()
                    .ForMember(dst => dst.Header, opt => opt.MapFrom(src => src.Head))
                    .ForMember(dst => dst.ColumnDeterminant, opt => opt.MapFrom(src => src.ColumnId))
                    .ForMember(dst => dst.RowDeterminant, opt => opt.MapFrom(src => src.RowId));

                cfg.CreateMap<ColumnViewModel, ColumnInfo>()
                    // incorrect .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                    .ForMember(dst => dst.Width, opt => opt.MapFrom(src => src.Size));

                cfg.CreateMap<ColumnInfo, ColumnViewModel>()
                    .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Width));

                cfg.CreateMap<RowViewModel, RowInfo>()
                    // incorrect: .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                    .ForMember(dst => dst.Height, opt => opt.MapFrom(src => src.Size));

                cfg.CreateMap<RowInfo, RowViewModel>()
                    .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Height));
            });
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
