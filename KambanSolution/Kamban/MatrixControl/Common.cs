using Kamban.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
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
        string Caption { get; set; }
        int Determinant { get; set; }
        int Size { get; set; }
        int Order { get; set; }
    }

    public class ColumnViewModel : ReactiveObject, IDim
    {
        public ColumnInfo Info { get; }

        public ColumnViewModel(ColumnInfo ci)
        {
            Info = ci;

            Id = ci.Id;
            Caption = ci.Name;
            Determinant = ci.Id;
            Size = ci.Width;
            Order = ci.Order;

            this.WhenAnyValue(x => x.Size).Subscribe(x => Info.Width = x);
            this.WhenAnyValue(x => x.Caption).Subscribe(x => Info.Name = x);
            this.WhenAnyValue(x => x.Order).Subscribe(x => Info.Order = x);
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public int Determinant { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
    }

    public class RowViewModel : ReactiveObject, IDim
    {
        public RowInfo Info { get; }

        public RowViewModel(RowInfo ri)
        {
            Info = ri;

            Id = ri.Id;
            Caption = ri.Name;
            Determinant = ri.Id;
            Size = ri.Height;
            Order = ri.Order;

            this.WhenAnyValue(x => x.Size).Subscribe(x => Info.Height = x);
            this.WhenAnyValue(x => x.Caption).Subscribe(x => Info.Name = x);
            this.WhenAnyValue(x => x.Order).Subscribe(x => Info.Order = x);
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public int Determinant { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
    }

    public interface ICard
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

    // def back color "#FFFFE0"
    public class CardViewModel : ReactiveObject, ICard
    {
        public CardViewModel()
        {
            // TODO: whenany => update modified

            this.WhenAnyValue(x => x.Body)
                .Subscribe(x => ShowDescription = !string.IsNullOrEmpty(x));
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Header { get; set; }
        [Reactive] public string Color { get; set; }
        [Reactive] public int ColumnDeterminant { get; set; }
        [Reactive] public int RowDeterminant { get; set; }
        [Reactive] public int Order { get; set; }
        [Reactive] public string Body { get; set; }
        [Reactive] public DateTime Created { get; set; }
        [Reactive] public DateTime Modified { get; set; }
        [Reactive] public int BoardId { get; set; }
        [Reactive] public bool ShowDescription { get; set; }
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
