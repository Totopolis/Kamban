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

        public void RebuildGrid()
        {
            MainGrid.Children.Clear();

            if (Rows.Count == 0 || Columns.Count == 0)
                return;

            //////////////////
            // 1. Fill columns
            //////////////////
            MainGrid.ColumnDefinitions.Clear();
            // rows header
            MainGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });

            // columns
            for (int i = 0; i < Columns.Count; i++)
            {
                var it = Columns[i];

                var cd = new ColumnDefinition();
                cd.DataContext = it;
                cd.Width = new GridLength(it.Size / 10, GridUnitType.Star);
                MainGrid.ColumnDefinitions.Add(cd);

                PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition));
                pd.AddValueChanged(cd, new EventHandler(ColumnWidthPropertyChanged));

                ContentControl cc = new ContentControl();
                cc.Content = it;
                cc.ContentTemplate = (DataTemplate)defaultTemplates["DefaultHorizontalHeaderTemplate"];
                MainGrid.Children.Add(cc);

                MainGrid.Children.Add(BuildHorizontalSpliter(i, Columns.Count));

                Grid.SetColumn(cc, i + 1);
                Grid.SetRow(cc, 0);
            }

            ///////////////
            // 2. Fill rows
            ///////////////
            MainGrid.RowDefinitions.Clear();
            // columns header
            MainGrid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(30, GridUnitType.Pixel) });
            // rows
            for (int i = 0; i < Rows.Count; i++)
            {
                var it = Rows[i];

                var rd = new RowDefinition();
                rd.DataContext = it;
                rd.Height = new GridLength(it.Size / 10, GridUnitType.Star);
                MainGrid.RowDefinitions.Add(rd);

                PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(RowDefinition.HeightProperty, typeof(RowDefinition));
                pd.AddValueChanged(rd, new EventHandler(RowWidthPropertyChanged));

                ContentControl cc = new ContentControl();
                cc.Content = it;
                cc.ContentTemplate = (DataTemplate)defaultTemplates["DefaulVerticalHeaderTemplate"];
                MainGrid.Children.Add(cc);

                MainGrid.Children.Add(BuildVerticalSpliter(i, Rows.Count));

                Grid.SetColumn(cc, 0);
                Grid.SetRow(cc, i + 1);
            }

            ////////////////////////
            // 3. Fill Intersections
            ////////////////////////
            cells = new Dictionary<int, Intersection>();
            cardPointers = new Dictionary<ICard, Intersection>();

            for (int i = 0; i < Columns.Count; i++)
                for (int j = 0; j < Rows.Count; j++)
                {
                    Intersection cell = new Intersection(this)
                    {
                        DataContext = this,
                        ColumnDeterminant = Columns[i].Determinant,
                        RowDeterminant = Rows[j].Determinant
                    };

                    int hash = GetHashValue(Columns[i].Determinant, Rows[j].Determinant);

                    cells.Add(hash, cell);

                    MainGrid.Children.Add(cell);
                    Grid.SetColumn(cell, i + 1);
                    Grid.SetColumnSpan(cell, 1);
                    Grid.SetRow(cell, j + 1);
                    Grid.SetRowSpan(cell, 1);
                }

            RebuildCards();
        }

        private int GetHashValue(object a, object b)
        {
            return new { a, b }.GetHashCode();
        }

        private void RebuildCards()
        {
            foreach (var it in Cards)
            {
                int hash = GetHashValue(it.ColumnDeterminant, it.RowDeterminant);

                if (!cells.ContainsKey(hash))
                    continue;

                cells[hash].SelfCards.Add(it);
                cardPointers.Add(it, cells[hash]);
            }
        }

        private GridSplitter BuildHorizontalSpliter(int index, int horizontalCategoriescount)
        {
            var newSpliter = new GridSplitter
            {
                ResizeDirection = GridResizeDirection.Rows,
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            Panel.SetZIndex(newSpliter, int.MaxValue);
            Grid.SetColumn(newSpliter, 0);
            Grid.SetRow(newSpliter, index + 1);
            Grid.SetColumnSpan(newSpliter, horizontalCategoriescount + 1);

            return newSpliter;
        }

        private GridSplitter BuildVerticalSpliter(int index, int verticalCategoriesCount)
        {
            var newSpliter = new GridSplitter
            {
                ResizeDirection = GridResizeDirection.Columns,
                Width = 1
            };

            Panel.SetZIndex(newSpliter, int.MaxValue);
            Grid.SetRow(newSpliter, 0);
            Grid.SetColumn(newSpliter, index + 1);
            Grid.SetRowSpan(newSpliter, verticalCategoriesCount + 1);

            return newSpliter;
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
