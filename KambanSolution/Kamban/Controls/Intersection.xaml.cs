using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Kamban.ViewModels.Core;
using ReactiveUI.Fody.Helpers;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Intersection.xaml
    /// </summary>
    public partial class Intersection : UserControl
    {
        private readonly Matrix mx;
        public Intersection(Matrix parent)
        {
            mx = parent;
            InitializeComponent();

            SelfCards = null;
            SelectedCard = null;
        }

        public int ColumnDeterminant { get; set; }
        public int RowDeterminant { get; set; }

        [Reactive] public ReadOnlyObservableCollection<ICard> SelfCards { get; set; }

        [Reactive] public ICard SelectedCard { get; set; }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(mainListView);
            SelectedCard = mainListView.GetObjectAtPoint<ListViewItem>(point) as ICard;
            mx.CardUnderMouse = SelectedCard;
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            mx.CardOfContextMenu = SelectedCard;
            e.Handled = SelectedCard == null;
        }

        private void mainListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(mainListView);
            var card = mainListView.GetObjectAtPoint<ListViewItem>(point) as ICard;

            if (card == null)
            {
                var tup = (ColumnDeterminant, RowDeterminant);
                mx.CellDoubleClickCommand?.Execute(tup).Subscribe();
            }
        }
    }//end of control

    public static class IntersectionHelper
    {
        public static object GetObjectAtPoint<ItemContainer>(this ItemsControl control, Point p)
            where ItemContainer : DependencyObject
        {
            // ItemContainer - can be ListViewItem, or TreeViewItem and so on(depends on control)
            ItemContainer obj = GetContainerAtPoint<ItemContainer>(control, p);
            if (obj == null)
                return null;

            return control.ItemContainerGenerator.ItemFromContainer(obj);
        }

        public static ItemContainer GetContainerAtPoint<ItemContainer>(this ItemsControl control, Point p)
        where ItemContainer : DependencyObject
        {
            HitTestResult result = VisualTreeHelper.HitTest(control, p);
            if (result == null)
                return null;

            DependencyObject obj = result.VisualHit;

            while (VisualTreeHelper.GetParent(obj) != null && !(obj is ItemContainer))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            // Will return null if not found
            return obj as ItemContainer;
        }
    }
}
