using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Kamban.MatrixControl
{
    public partial class Matrix
    {
        public void RebuildGrid()
        {
            Monik?.ApplicationVerbose("Matrix.RebuildGrid started");

            MainGrid.Children.Clear();

            if (Rows == null || Columns == null ||
                Rows.Count == 0 || Columns.Count == 0)
            {
                Monik?.ApplicationVerbose("Matrix.RebuildGrid skip func");
                return;
            }

            var columns = Columns.ToList();
            int columnCount = Columns.Count;
            var rows = Rows.ToList();
            int rowCount = Rows.Count;

            //////////////////
            // 1. Fill columns
            //////////////////
            MainGrid.ColumnDefinitions.Clear();
            // rows header
            MainGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });

            // columns
            for (int i = 0; i < columnCount; i++)
            {
                var it = columns[i];
                Monik?.ApplicationVerbose($"Matrix.RebuildGrid add column {it.Id}::{it.Name}::{it.Order}");

                var cd = new ColumnDefinition();
                cd.DataContext = it;
                cd.Width = new GridLength(it.Size / 10, GridUnitType.Star);
                MainGrid.ColumnDefinitions.Add(cd);

                PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition));
                pd.AddValueChanged(cd, new EventHandler(ColumnWidthPropertyChanged));

                ContentControl cc = new ContentControl();
                cc.Content = it;
                cc.MouseMove += Head_MouseMove;
                cc.ContextMenu = HeadContextMenu;
                cc.ContentTemplate = (DataTemplate)this.Resources["DefaultHorizontalHeaderTemplate"];
                MainGrid.Children.Add(cc);

                // Update number of Cards in Column
                CardsObservable
                    .Filter(x => x.ColumnDeterminant == it.Id)
                    .Subscribe(y =>  it.CurNumberOfCards += y.Adds - y.Removes  );
                                                          

                // dont draw excess splitter
                if (i < columnCount - 1)
                    MainGrid.Children.Add(BuildVerticalSpliter(i, rowCount));

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
            for (int i = 0; i < rowCount; i++)
            {
                var it = rows[i];
                Monik?.ApplicationVerbose($"Matrix.RebuildGrid add row {it.Id}::{it.Name}::{it.Order}");

                var rd = new RowDefinition();
                rd.DataContext = it;
                rd.Height = new GridLength(it.Size / 10, GridUnitType.Star);
                MainGrid.RowDefinitions.Add(rd);

                PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(RowDefinition.HeightProperty, typeof(RowDefinition));
                pd.AddValueChanged(rd, new EventHandler(RowWidthPropertyChanged));

                ContentControl cc = new ContentControl();
                cc.Content = it;
                cc.MouseMove += Head_MouseMove;
                cc.ContextMenu = HeadContextMenu;
                cc.ContentTemplate = (DataTemplate)this.Resources["DefaulVerticalHeaderTemplate"];
                MainGrid.Children.Add(cc);


                // Update number of Cards in Column
                CardsObservable
                    .Filter(x => x.RowDeterminant == it.Id)
                    .Subscribe(y => it.CurNumberOfCards += y.Adds - y.Removes);


                // dont draw excess splitter
                if (i < rowCount - 1)
                    MainGrid.Children.Add(BuildHorizontalSpliter(i, columnCount));

                Grid.SetColumn(cc, 0);
                Grid.SetRow(cc, i + 1);
            }

            ////////////////////////
            // 3. Fill Intersections
            ////////////////////////
            for (int i = 0; i < Columns.Count; i++)
                for (int j = 0; j < Rows.Count; j++)
                {
                    int colDet = columns[i].Id;
                    int rowDet = rows[j].Id;

                    CardsObservable
                        .Filter(x => x.ColumnDeterminant == colDet && x.RowDeterminant == rowDet)
                        .Sort(SortExpressionComparer<ICard>.Ascending(c => c.Order))
                        .ObserveOnDispatcher()
                        .Bind(out ReadOnlyObservableCollection<ICard> intersectionCards)
                        .Subscribe();

                    Intersection cell = new Intersection(this)
                    {
                        DataContext = this,
                        ColumnDeterminant = colDet,
                        RowDeterminant = rowDet,
                        SelfCards = intersectionCards
                    };

                    MainGrid.Children.Add(cell);
                    Grid.SetColumn(cell, i + 1);
                    Grid.SetColumnSpan(cell, 1);
                    Grid.SetRow(cell, j + 1);
                    Grid.SetRowSpan(cell, 1);
                }

            Monik?.ApplicationVerbose("Matrix.RebuildGrid finished");
        }

        private int GetHashValue(object a, object b)
        {
            return new { a, b }.GetHashCode();
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
            Grid.SetColumnSpan(newSpliter, horizontalCategoriescount + 1);

            Grid.SetRow(newSpliter, index + 1);

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
            Grid.SetRowSpan(newSpliter, verticalCategoriesCount + 1);

            Grid.SetColumn(newSpliter, index + 1);
            
            return newSpliter;
        }

    }//end of class
}
