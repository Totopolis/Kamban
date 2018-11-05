using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Row or column description
    /// </summary>
    public interface IDim
    {
        string Name { get; set; }
        int Determinant { get; set; }
        int Size { get; set; }
        int Order { get; set; }
    }

    public interface ICard : INotifyPropertyChanged
    {
        int Id { get; set; }
        string Header { get; set; }
        string Color { get; set; }

        int ColumnDeterminant { get; set; }
        int RowDeterminant { get; set; }
        int Order { get; set; }

        string Body { get; set; }
        DateTime Created { get; set; }
        DateTime Modified { get; set; }

        int BoardId { get; set; }
        bool ShowDescription { get; set; }
    }

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
