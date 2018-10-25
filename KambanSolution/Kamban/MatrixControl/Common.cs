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
        object Determinant { get; set; }
        int Size { get; set; }
        int Order { get; set; }
    }

    public class ColumnViewModel : ReactiveObject, IDim
    {
        private readonly ColumnInfo columnInfo;
        public ColumnInfo Info => columnInfo;

        public ColumnViewModel(ColumnInfo ci)
        {
            columnInfo = ci;

            Id = ci.Id;
            Caption = ci.Name;
            Determinant = ci.Id;
            Size = ci.Width;
            Order = ci.Order;

            this.WhenAnyValue(x => x.Size).Subscribe(x => columnInfo.Width = x);
            this.WhenAnyValue(x => x.Caption).Subscribe(x => columnInfo.Name = x);
            this.WhenAnyValue(x => x.Order).Subscribe(x => columnInfo.Order = x);
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public object Determinant { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
    }

    public class RowViewModel : ReactiveObject, IDim
    {
        private readonly RowInfo rowInfo;
        public RowInfo Info => rowInfo;

        public RowViewModel(RowInfo ri)
        {
            rowInfo = ri;

            Id = ri.Id;
            Caption = ri.Name;
            Determinant = ri.Id;
            Size = ri.Height;
            Order = ri.Order;

            this.WhenAnyValue(x => x.Size).Subscribe(x => rowInfo.Height = x);
            this.WhenAnyValue(x => x.Caption).Subscribe(x => rowInfo.Name = x);
            this.WhenAnyValue(x => x.Order).Subscribe(x => rowInfo.Order = x);
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Caption { get; set; }
        [Reactive] public object Determinant { get; set; }
        [Reactive] public int Size { get; set; }
        [Reactive] public int Order { get; set; }
    }

    public interface ICard
    {
        int Id { get; set; }
        string Header { get; set; }
        string Color { get; set; }

        object ColumnDeterminant { get; set; }
        object RowDeterminant { get; set; }
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
        private readonly Issue issueInfo;
        public Issue Issue => issueInfo;

        public CardViewModel(Issue iss)
        {
            issueInfo = iss;

            Id = iss.Id;
            Header = iss.Head;
            Color = iss.Color;
            ColumnDeterminant = iss.ColumnId;
            RowDeterminant = iss.RowId;
            Order = iss.Order;
            Body = iss.Body;
            Created = iss.Created;
            Modified = iss.Modified;
            ShowDescription = !string.IsNullOrEmpty(issueInfo.Body);
            BoardId = iss.BoardId;

            this.WhenAnyValue(x => x.Header).Subscribe(x => issueInfo.Head = x);
            this.WhenAnyValue(x => x.Color).Subscribe(x => issueInfo.Color = x);

            this.WhenAnyValue(x => x.Body).Subscribe(x =>
            {
                ShowDescription = !string.IsNullOrEmpty(x);
                issueInfo.Body = x;
            });

            this.WhenAnyValue(x => x.ColumnDeterminant).Subscribe(x => issueInfo.ColumnId = (int)x);
            this.WhenAnyValue(x => x.RowDeterminant).Subscribe(x => issueInfo.RowId = (int)x);
            this.WhenAnyValue(x => x.Order).Subscribe(x => issueInfo.Order = x);
            this.WhenAnyValue(x => x.Modified).Subscribe(x => issueInfo.Modified = x);
        }

        [Reactive] public int Id { get; set; }
        [Reactive] public string Header { get; set; }
        [Reactive] public string Color { get; set; }
        [Reactive] public object ColumnDeterminant { get; set; }
        [Reactive] public object RowDeterminant { get; set; }
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
