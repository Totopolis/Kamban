using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Legacy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Reactive;



namespace Kamban.MatrixControl
{
    public partial class MatrixForExport
    {
        public void RebuildGrid()
        {
            MainGrid.Children.Clear();

            if (Rows == null || Columns == null ||
                !Rows.Any() || !Columns.Any())
                return;
            
            var columnCount = Columns.Length;
            var rowCount = Rows.Length;

            //////////////////
            // 1. Fill columns
            //////////////////
            MainGrid.ColumnDefinitions.Clear();
            // rows header
            MainGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });

            // columns
            for (var i = 0; i < columnCount; i++)
            {
                var it = Columns[i];
                
                var cd = new ColumnDefinition
                {
                    DataContext = it,
                    Width = new GridLength(it.Size / 10, GridUnitType.Star)
                };
                MainGrid.ColumnDefinitions.Add(cd);

                var cc = new ContentControl
                {
                    Content = it,
                    ContentTemplate = (DataTemplate) Resources["DefaultHorizontalHeaderTemplate"]
                };
                MainGrid.Children.Add(cc);

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
            for (var i = 0; i < rowCount; i++)
            {
                var it = Rows[i];

                var rd = new RowDefinition
                {
                    DataContext = it,
                    Height = new GridLength(it.Size / 10, GridUnitType.Star)
                };
                MainGrid.RowDefinitions.Add(rd);

                var cc = new ContentControl
                {
                    Content = it,
                    ContentTemplate = (DataTemplate) Resources["DefaulVerticalHeaderTemplate"]
                };
                MainGrid.Children.Add(cc);

                // dont draw excess splitter
                if (i < rowCount - 1)
                    MainGrid.Children.Add(BuildHorizontalSpliter(i, columnCount));

                Grid.SetColumn(cc, 0);
                Grid.SetRow(cc, i + 1);
            }

            ////////////////////////
            // 3. Fill Intersections
            ////////////////////////
            for (var i = 0; i < columnCount; i++)
                for (var j = 0; j < rowCount; j++)
                {
                    var cell = new IntersectionForExport
                    {
                        DataContext = this,
                        SelfCards = Cards
                            .Where(x => x.ColumnDeterminant == Columns[i].Id 
                                        && x.RowDeterminant == Rows[j].Id)
                            .OrderBy(c => c.Order)
                            .ToArray()
                    };

                    MainGrid.Children.Add(cell);
                    Grid.SetColumn(cell, i + 1);
                    Grid.SetColumnSpan(cell, 1);
                    Grid.SetRow(cell, j + 1);
                    Grid.SetRowSpan(cell, 1);
                }
        }

        private static GridSplitter BuildHorizontalSpliter(int index, int horizontalCategoriescount)
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

        private static GridSplitter BuildVerticalSpliter(int index, int verticalCategoriesCount)
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
