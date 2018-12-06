using Autofac;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.Core
{
    public class ShellEx : Shell
    {
        private static readonly Action EmptyDelegate = delegate { };

        public BitmapSource RenderView<TView>(ViewRequest viewRequest,
            int dpi = 72, double? width = null, double? height = null)
            where TView : FrameworkElement, IView
        {
            var view = Container.Resolve<TView>();

            if (view.ViewModel is IInitializableViewModel initializibleViewModel)
            {
                initializibleViewModel.Initialize(viewRequest);
            }

            if (view.ViewModel is IActivatableViewModel activatableViewModel)
            {
                activatableViewModel.Activate(viewRequest);
            }

            view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            var renderWidth = (int) Math.Ceiling(width * dpi ?? view.ActualWidth);
            var renderHeight = (int) Math.Ceiling(height * dpi ?? view.ActualHeight);
            var bounds = new Rect(0, 0, renderWidth, renderHeight);

            view.Measure(bounds.Size);
            view.Arrange(bounds);
            view.UpdateLayout();

            var rtb = new RenderTargetBitmap(
                renderWidth,
                renderHeight,
                96, 96,
                PixelFormats.Pbgra32);

            rtb.Render(view);

            return rtb;
        }


        public void PrintView<TView>(IEnumerable<ViewRequest> viewRequests)
            where TView : FrameworkElement, IView
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            var capabilities = pd.PrintQueue.GetPrintCapabilities(pd.PrintTicket);

            var pageSize = new Size(
                capabilities.PageImageableArea.ExtentWidth,
                capabilities.PageImageableArea.ExtentHeight
            );

            var document = new FixedDocument();

            foreach (var viewRequest in viewRequests)
            {
                var view = Container.Resolve<TView>();
                
                if (view.ViewModel is IInitializableViewModel initializibleViewModel)
                {
                    initializibleViewModel.Initialize(viewRequest);
                }

                if (view.ViewModel is IActivatableViewModel activatableViewModel)
                {
                    activatableViewModel.Activate(viewRequest);
                }

                view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                var scale = Math.Min(
                    pageSize.Width / view.DesiredSize.Width,
                    pageSize.Height / view.DesiredSize.Height
                );

                view.LayoutTransform = new ScaleTransform(scale, scale);
                //view.Width = view.DesiredSize.Width;
                //view.Height = pageSize.Height / scale;
                
                var page = new FixedPage();
                page.Children.Add(view);
                var pageContent = new PageContent {Child = page};
                document.Pages.Add(pageContent);
            }

            pd.PrintDocument(document.DocumentPaginator, "");
        }
    }
}