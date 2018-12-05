using Autofac;
using System;
using System.Windows;
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

        public BitmapSource RenderView<TView>(ViewRequest viewRequest, int dpi = 72, double? width = null, double? height = null)
            where TView : class, IView
        {
            var view = Container.Resolve<TView>();
            
            if (!(view is FrameworkElement))
                return null;

            var element = view as FrameworkElement;

            if (view.ViewModel is IInitializableViewModel initializibleViewModel)
            {
                initializibleViewModel.Initialize(viewRequest);
            }

            if (view.ViewModel is IActivatableViewModel activatableViewModel)
            {
                activatableViewModel.Activate(viewRequest);
            }

            element.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            var renderWidth = (int)Math.Ceiling(width * dpi ?? element.ActualWidth);
            var renderHeight = (int)Math.Ceiling(height * dpi ?? element.ActualHeight);
            var bounds = new Rect(0, 0, renderWidth, renderHeight);

            element.Measure(bounds.Size);
            element.Arrange(bounds);
            element.UpdateLayout();

            var rtb = new RenderTargetBitmap(
                renderWidth,
                renderHeight,
                96, 96,
                PixelFormats.Pbgra32);

            rtb.Render(element);

            return rtb;
        }

    }
}
