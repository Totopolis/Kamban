using System.Windows;

namespace Kamban.MatrixControl
{
    public static class ContextMenuServiceExtensions
    {
        public static readonly DependencyProperty DataContextProperty =
            DependencyProperty.RegisterAttached("DataContext",
            typeof(object), typeof(ContextMenuServiceExtensions),
            new UIPropertyMetadata(DataContextChanged));

        public static object GetDataContext(FrameworkElement obj)
        {
            return obj.GetValue(DataContextProperty);
        }

        public static void SetDataContext(FrameworkElement obj, object value)
        {
            obj.SetValue(DataContextProperty, value);
        }

        private static void DataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Matrix m = d as Matrix;
            if (m == null)
                return;

            var parent = (m?.Parent as FrameworkElement);//?.Parent as FrameworkElement;

            if (m.CardContextMenu != null)
                m.CardContextMenu.DataContext = parent.DataContext; //GetDataContext(parent);

            if (m.HeadContextMenu != null)
                m.HeadContextMenu.DataContext = parent.DataContext;
        }
    }//end of class
}
