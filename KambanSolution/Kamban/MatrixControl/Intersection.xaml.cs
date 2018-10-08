using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            SelfCards = new ReactiveList<ICard>();
            SelectedCard = null;
        }

        public object ColumnDeterminant { get; set; }
        public object RowDeterminant { get; set; }

        public ReactiveList<ICard> SelfCards { get; set; }

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
