using GongSolutions.Wpf.DragDrop;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
        public static void OnCardsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;

            mx.RebuildGrid();

            // TODO: concrete add or remove
            mx.Cards?
                .Changed
                .Subscribe(_ => mx.RebuildGrid());

            mx.Cards?
                .ItemChanged
                .Subscribe((x) =>
                {
                    var card = x.Sender;

                    // CRASH AT REMOVE COLUMN WITH CARDS IF OLD CELLS DELETED BY COL_ROW DELETE COMMAND!!!!!!!!!!!
                    var oldIntersec = mx.cardPointers[card];
                    var newIntersec = mx.cells[mx.GetHashValue(card.ColumnDeterminant, card.RowDeterminant)];

                    if (oldIntersec != newIntersec)
                    {
                        oldIntersec.SelfCards.Remove(card);
                        newIntersec.SelfCards.Add(card);
                        mx.cardPointers[card] = newIntersec;
                    }
                });
        }

        public static void OnColumnsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Columns
                .Changed
                .Subscribe(_ => mx.RebuildGrid());
        }

        public static void OnRowsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Rows
                .Changed
                .Subscribe(_ => mx.RebuildGrid());
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
                var cd = MainGrid.ColumnDefinitions;
                for (int i = 1; i < cd.Count; i++)
                    Columns[i - 1].Size = (int)cd[i].Width.Value;
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
                var rd = MainGrid.RowDefinitions;
                for (int i = 1; i < rd.Count; i++)
                    Rows[i - 1].Size = (int)rd[i].Height.Value;
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

                var colDefs = self.MainGrid.ColumnDefinitions;
                for (int i = 1; i < colDefs.Count; i++)
                {
                    var len = new GridLength(colSize, GridUnitType.Star);
                    colDefs[i].Width = len;
                    self.Columns[i - 1].Size = (int)len.Value * 10;
                }

                //self.GridRowsReset();
                if (self.Rows.Count <= 1)
                    return;

                double rowSize = 100 / (self.Rows.Count - 1);

                var rowDefs = self.MainGrid.RowDefinitions;
                for (int i = 1; i < rowDefs.Count; i++)
                {
                    var len = new GridLength(rowSize, GridUnitType.Star);
                    rowDefs[i].Height = len;
                    self.Rows[i - 1].Size = (int)len.Value * 10;
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
            var card = dropInfo.Data as CardViewModel;

            var srcIntersec = cardPointers[card];

            // dirty fingers
            var targetIntersec = ((dropInfo.VisualTarget as ListView)
                .Parent as Grid)
                .Parent as Intersection;

            if (targetIntersec != srcIntersec)
            {
                card.ColumnDeterminant = targetIntersec.ColumnDeterminant;
                card.RowDeterminant = targetIntersec.RowDeterminant;

                DropCardCommand?.Execute(card).Subscribe();
            }
        }

    }//end of class
}
