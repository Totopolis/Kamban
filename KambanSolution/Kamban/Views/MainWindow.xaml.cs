using System;
using Ui.Wpf.Common;

namespace Kamban.Views
{
    public partial class MainWindow : IDockWindow
    {
        public MainWindow(IShell shell)
        {
            Shell = shell;
            DataContext = Shell;
            InitializeComponent();
        }

        private IShell Shell { get; }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Shell.AttachDockingManager(DockingManager);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Shell?.Container.Dispose();
        }
    }
}
