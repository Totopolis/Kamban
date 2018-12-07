using Autofac;
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

        public FixedDocument ViewsToDocument<TView>(IEnumerable<ViewRequest> viewRequests, Size pageSize)
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

                var scale = Math.Min(
                    pageSize.Width / view.DesiredSize.Width,
                    pageSize.Height / view.DesiredSize.Height
                );

                view.LayoutTransform = new ScaleTransform(scale, scale);

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