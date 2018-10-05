using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ui.Wpf.KanbanControl;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Kanban.xaml
    /// </summary>
    public partial class Matrix : UserControl
    {
        private static readonly DefaultTemplates defaultTemplates = new DefaultTemplates();
        private Dictionary<int, Intersection> cells;

        public Matrix()
        {
            InitializeComponent();

            RebuildGrid();
        }

        public ReactiveList<ICard> Cards
        {
            get => (ReactiveList<ICard>)GetValue(CardsProperty);
            set => SetValue(CardsProperty, value);
        }

        public static readonly DependencyProperty CardsProperty =
            DependencyProperty.Register("Cards",
                typeof(ReactiveList<ICard>),
                typeof(Matrix),
                new PropertyMetadata(new ReactiveList<ICard>(),
                    new PropertyChangedCallback(OnCardsPropertyChanged)));

        public static void OnCardsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Cards
                .Changed
                .Subscribe(_ => mx.RebuildGrid());
        }

        public ReactiveList<IDim> Columns
        {
            get => (ReactiveList<IDim>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns",
                typeof(ReactiveList<IDim>),
                typeof(Matrix),
                new PropertyMetadata(new ReactiveList<IDim>(),
                    new PropertyChangedCallback(OnColumnsPropertyChanged)));

        public static void OnColumnsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Columns
                .Changed
                .Subscribe(_ => mx.RebuildGrid());
        }

        public ReactiveList<IDim> Rows
        {
            get => (ReactiveList<IDim>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows",
                typeof(ReactiveList<IDim>),
                typeof(Matrix),
                new PropertyMetadata(new ReactiveList<IDim>(),
                    new PropertyChangedCallback(OnRowsPropertyChanged)));

        public static void OnRowsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = obj as Matrix;
            mx.Rows
                .Changed
                .Subscribe(_ => mx.RebuildGrid());
        }

        public ReactiveCommand<CardViewModel, Unit> CardClickCommand
        {
            get => (ReactiveCommand<CardViewModel, Unit>)GetValue(CardClickCommandProperty);
            set => SetValue(CardClickCommandProperty, value);
        }

        public static readonly DependencyProperty CardClickCommandProperty =
            DependencyProperty.Register(
                "CardClickCommand",
                typeof(ReactiveCommand<CardViewModel, Unit>),
                typeof(Matrix),
                new PropertyMetadata(null));

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
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            // columns
            for (int i = 0; i < Columns.Count; i++)
            {
                var it = Columns[i];

                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());

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
            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // rows
            for (int i = 0; i < Rows.Count; i++)
            {
                var it = Rows[i];

                MainGrid.RowDefinitions.Add(new RowDefinition());

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

            for (int i = 0; i < Columns.Count; i++)
                for (int j = 0; j < Rows.Count; j++)
                {
                    Intersection cell = new Intersection();
                    cell.DataContext = this;

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
            ////////////////
            // 4. Fill Cards
            ////////////////
            foreach (var it in Cards)
            {
                int hash = GetHashValue(it.ColumnDeterminant, it.RowDeterminant);
                cells[hash].Cards.Add(it);
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

    }//end of class
}
