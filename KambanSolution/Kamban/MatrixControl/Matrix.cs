using DynamicData;
using GongSolutions.Wpf.DragDrop;
using Kamban.Model;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Kamban.MatrixControl
{
    public partial class Matrix
    {
        public static void OnEnableWorkPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Monik?.ApplicationVerbose("Matrix.OnEnableWorkPropertyChanged");

            if (mx.EnableWork)
                mx.RebuildGrid();
        }

        public static void OnCardsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Monik?.ApplicationVerbose("Matrix.OnCardsPropertyChanged");
        }

        public static void OnColumnsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Monik?.ApplicationVerbose("Matrix.OnColumnsPropertyChanged");

            if (mx.Columns == null)
                return;
        }

        public static void OnRowsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Monik?.ApplicationVerbose("Matrix.OnRowsPropertyChanged");

            if (mx.Rows == null)
                return;
        }

        private ColumnDefinition columnWidthChanging = null;
        private void ColumnWidthPropertyChanged(object sender, EventArgs e)
        {
            // listen for when the mouse is released
            columnWidthChanging = sender as ColumnDefinition;
            if (sender != null)
                Mouse.AddPreviewMouseUpHandler(this, ColumnResize_MouseLeftButtonUp);
        }

        void ColumnResize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (columnWidthChanging != null)
            {
                var columns = Columns.ToList();
                var cd = MainGrid.ColumnDefinitions;
                for (int i = 1; i < cd.Count; i++)
                    columns[i - 1].Size = (int)cd[i].Width.Value;
            }
        }

        private RowDefinition rowHeightChanging = null;
        private void RowWidthPropertyChanged(object sender, EventArgs e)
        {
            // listen for when the mouse is released
            rowHeightChanging = sender as RowDefinition;
            if (sender != null)
                Mouse.AddPreviewMouseUpHandler(this, RowResize_MouseLeftButtonUp);
        }

        void RowResize_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (rowHeightChanging != null)
            {
                var rows = Rows.ToList();
                var rd = MainGrid.RowDefinitions;
                for (int i = 1; i < rd.Count; i++)
                    rows[i - 1].Size = (int)rd[i].Height.Value;
            }
        }

        public static void NormalizeGridCommandPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var self = obj as Matrix;
            var cmd = args.NewValue as ReactiveCommand<Unit, Unit>;
            if (cmd == null)
                return;

            cmd.Subscribe(_ =>
            {
                //self.GridColumnsReset();
                if (self.Columns.Count <= 1)
                    return;

                double colSize = 100 / (self.Columns.Count - 1);

                var columns = self.Columns.ToList();
                var colDefs = self.MainGrid.ColumnDefinitions;
                for (int i = 1; i < colDefs.Count; i++)
                {
                    var len = new GridLength(colSize, GridUnitType.Star);
                    colDefs[i].Width = len;
                    columns[i - 1].Size = (int)len.Value * 10;
                }

                //self.GridRowsReset();
                if (self.Rows.Count <= 1)
                    return;

                double rowSize = 100 / (self.Rows.Count - 1);

                var rows = self.Rows.ToList();
                var rowDefs = self.MainGrid.RowDefinitions;
                for (int i = 1; i < rowDefs.Count; i++)
                {
                    var len = new GridLength(rowSize, GridUnitType.Star);
                    rowDefs[i].Height = len;
                    rows[i - 1].Size = (int)len.Value * 10;
                }
            });
        }

        private void Head_MouseMove(object sender, MouseEventArgs e)
        {
            var ic = sender as ContentControl;
            if (ic == null) return;
            var point = e.GetPosition(ic);
            HitTestResult result = VisualTreeHelper.HitTest(ic, point);

            var border = result.VisualHit as Border;
            if (border == null) return;
            HeadOfContextMenu = border.DataContext;
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Move;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            Monik?.ApplicationVerbose("Matrix.Drop");

            var card = dropInfo.Data as CardViewModel;
            var targetCard = dropInfo.TargetItem as CardViewModel;

            // dirty fingers
            var targetIntersec = ((dropInfo.VisualTarget as ListView)
                .Parent as Grid)
                .Parent as Intersection;

            if (card.ColumnDeterminant != targetIntersec.ColumnDeterminant ||
                card.RowDeterminant != targetIntersec.RowDeterminant)
            {
                Monik?.ApplicationVerbose("Matrix.Drop move to other intersection");
                card.ColumnDeterminant = targetIntersec.ColumnDeterminant;
                card.RowDeterminant = targetIntersec.RowDeterminant;
            }

            // Reorder
            card.Order = targetCard != null ? targetCard.Order - 1 : int.MaxValue;

            // OPTIMiZE: Use R/O/ObservableCollection projection

            var targetCards = Cards.Items
                        .Where(x => x.ColumnDeterminant == card.ColumnDeterminant
                            && x.RowDeterminant == card.RowDeterminant)
                        .OrderBy(x => x.Order);

            int i = 0;
            foreach (var it in targetCards)
            {
                it.Order = i;
                i += 10;
            }
        }

    }//end of class
}
