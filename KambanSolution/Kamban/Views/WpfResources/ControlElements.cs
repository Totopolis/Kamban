using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kamban.Views.WpfResources
{
    public class MyScrollViewer : ScrollViewer
    {
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Parent is UIElement parentElement)
            {
                if ((e.Delta > 0 && VerticalOffset == 0) ||
                    (e.Delta < 0 && VerticalOffset == ScrollableHeight))
                {
                    e.Handled = true;

                    var routedArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    routedArgs.RoutedEvent = MouseWheelEvent;
                    parentElement.RaiseEvent(routedArgs);
                }
            }
            base.OnMouseWheel(e);
        }
    }
}
