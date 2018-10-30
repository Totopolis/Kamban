using Autofac;
using Kamban.Model;
using Kamban.Views;
using Kamban.ViewModels;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using Kamban.Repository;
using MahApps.Metro.Controls.Dialogs;
using Monik.Common;
using AutoMapper;
using Kamban.MatrixControl;

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

            var app = container.Resolve<IAppModel>();
            app.Initialize();

            return shell;
        }

        private static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<Shell>()
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

            ConfigureView<StartupViewModel, StartupView>(builder);
            ConfigureView<WizardViewModel, WizardView>(builder);
            ConfigureView<BoardEditViewModel, BoardView>(builder);
            ConfigureView<ExportViewModel, ExportView>(builder);

            return builder.Build();
        }

        private static MapperConfiguration ConfigureMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<CardViewModel, Issue>()
                    .ForMember(dst => dst.Head, opt => opt.MapFrom(src => src.Header))
                    .ForMember(dst => dst.ColumnId, opt => opt.MapFrom(src => src.ColumnDeterminant))
                    .ForMember(dst => dst.RowId, opt => opt.MapFrom(src => src.RowDeterminant));

                cfg.CreateMap<Issue, CardViewModel>()
                    .ForMember(dst => dst.Header, opt => opt.MapFrom(src => src.Head))
                    .ForMember(dst => dst.ColumnDeterminant, opt => opt.MapFrom(src => src.ColumnId))
                    .ForMember(dst => dst.RowDeterminant, opt => opt.MapFrom(src => src.RowId));

                cfg.CreateMap<ColumnViewModel, ColumnInfo>()
                    .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Caption))
                    .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                    .ForMember(dst => dst.Width, opt => opt.MapFrom(src => src.Size));

                cfg.CreateMap<ColumnInfo, ColumnViewModel>()
                    .ForMember(dst => dst.Caption, opt => opt.MapFrom(src => src.Name))
                    .ForMember(dst => dst.Determinant, opt => opt.MapFrom(src => src.Id))
                    .ForMember(dst => dst.Size, opt => opt.MapFrom(src => src.Width));

                cfg.CreateMap<RowViewModel, RowInfo>()
                    .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Caption))
                    .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Determinant))
                    .ForMember(dst => dst.Height, opt => opt.MapFrom(src => src.Size));

                cfg.CreateMap<RowInfo, RowViewModel>()
                    .ForMember(dst => dst.Caption, opt => opt.MapFrom(src => src.Name))
                    .ForMember(dst => dst.Determinant, opt => opt.MapFrom(src => src.Id))
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
    }
}
