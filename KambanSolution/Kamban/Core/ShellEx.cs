using Autofac;
using Kamban.Export.Options;
using Kamban.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Ui.Wpf.Common;
using Ui.Wpf.Common.ViewModels;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace Kamban.Core
{
    public class ShellEx : Shell
    {
        private static readonly Action EmptyDelegate = delegate { };

        public FixedDocument ViewsToDocument<TView>(IEnumerable<ViewRequest> viewRequests, Size pageSize, ScaleOptions scaleOptions = null)
            where TView : FrameworkElement, IView
        {
            scaleOptions = scaleOptions ?? new ScaleOptions
            {
                Padding = new Thickness(),
                ScaleToFit = true,
                ScaleFitting = ScaleFitting.BothDirections,
                MaxScale = 1.0,
                MinScale = 0.0
            };

            var viewSize = new Size(
                pageSize.Width - (scaleOptions.Padding.Left + scaleOptions.Padding.Right),
                pageSize.Height - (scaleOptions.Padding.Top + scaleOptions.Padding.Bottom)
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
                view.Arrange(new Rect(0, 0, view.DesiredSize.Width, view.DesiredSize.Height));

                view.RenderTransform = new TranslateTransform(scaleOptions.Padding.Left, scaleOptions.Padding.Top);

                if (scaleOptions.ScaleToFit)
                {
                    var viewStretched = view as IStretchedSize;

                    var scaleX = viewStretched != null
                        ? viewSize.Width / viewStretched.StretchedWidth
                        : viewSize.Width / view.ActualWidth;
                    var scaleY = viewStretched != null
                        ? viewSize.Height / viewStretched.StretchedHeight
                        : viewSize.Height / view.ActualHeight;

                    var scale = scaleOptions.ScaleFitting.HasFlag(ScaleFitting.BothDirections)
                        ? Math.Min(scaleX, scaleY)
                        : scaleOptions.ScaleFitting.HasFlag(ScaleFitting.Horizontal)
                            ? scaleX
                            : scaleOptions.ScaleFitting.HasFlag(ScaleFitting.Vertical)
                                ? scaleY
                                : 1.0;

                    scale = Math.Min(scaleOptions.MaxScale, Math.Max(scaleOptions.MinScale, scale));

                    if (Math.Abs(scale - 1.0) > 0.001)
                    {
                        view.LayoutTransform = new ScaleTransform(scale, scale, scaleOptions.Padding.Left, scaleOptions.Padding.Top);
                        view.Width = Math.Ceiling(viewSize.Width / scale);
                        view.Height = Math.Ceiling(viewSize.Height / scale);
                    }
                    else
                    {
                        view.Width = viewSize.Width;
                        view.Height = viewSize.Height;
                    }
                }
                else
                {
                    view.Width = viewSize.Width;
                    view.Height = viewSize.Height;
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