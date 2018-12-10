using Autofac;
using Kamban.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;

namespace Kamban.Core
{
    public class ShellEx : Shell
    {
        private static readonly Action EmptyDelegate = delegate { };

        public FixedDocument ViewsToDocument<TView>(IEnumerable<ViewRequest> viewRequests, Size pageSize, bool stretch = true)
            where TView : FrameworkElement, IView
        {
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
                view.Arrange(new Rect(0, 0, view.DesiredSize.Width, view.DesiredSize.Height));

                var scale =
                    stretch && view is IStretchedSize viewStretched
                        ? Math.Min(
                            pageSize.Width / viewStretched.StretchedWidth,
                            pageSize.Height / viewStretched.StretchedHeight
                        )
                        : Math.Min(
                            pageSize.Width / view.ActualWidth,
                            pageSize.Height / view.ActualHeight
                        );

                view.LayoutTransform = new ScaleTransform(scale, scale);
                if (stretch)
                {
                    view.Width = Math.Ceiling(pageSize.Width / scale);
                    view.Height = Math.Ceiling(pageSize.Height / scale);
                }

                var page = new FixedPage();
                page.Children.Add(view);
                page.Width = pageSize.Width;
                page.Height = pageSize.Height;
                var pageContent = new PageContent { Child = page };
                document.Pages.Add(pageContent);
            }

            return document;
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

            var document = ViewsToDocument<TView>(viewRequests, pageSize);

            pd.PrintDocument(document.DocumentPaginator, "");
        }
    }
}