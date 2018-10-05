using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
        private Dictionary<ICard, Intersection> cardPointers;

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

            mx.RebuildGrid();

            mx.Cards?
                .Changed
                .Subscribe(_ => mx.RebuildGrid());

            mx.Cards?
                .ItemChanged
                .Subscribe((x) =>
                {
                    var card = x.Sender;
                    var oldInter = mx.cardPointers[card];
                    var newInter = mx.cells[mx.GetHashValue(card.ColumnDeterminant, card.RowDeterminant)];

                    if (oldInter != newInter)
                    {
                        oldInter.Cards.Remove(card);
                        newInter.Cards.Add(card);
                        mx.cardPointers[card] = newInter;
                    }
                });
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
                UpdateGridColumnsModel();
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
                UpdateGridRowsModel();
        }

        public ReactiveCommand<Unit, Unit> NormalizeGridCommand
        {
            get => (ReactiveCommand<Unit, Unit>)GetValue(NormalizeGridCommandProperty);
            set => SetValue(NormalizeGridCommandProperty, value);
        }

        public static readonly DependencyProperty NormalizeGridCommandProperty =
            DependencyProperty.Register(
                "NormalizeGridCommand",
                typeof(ReactiveCommand<Unit, Unit>),
                typeof(Matrix),
                new PropertyMetadata(null, new PropertyChangedCallback(NormalizeGridCommandPropertyChanged)));

        public static void NormalizeGridCommandPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var self = obj as Matrix;
            var cmd = args.NewValue as ReactiveCommand<Unit, Unit>;
            if (cmd == null)
                return;

            cmd.Subscribe(_ =>
            {
                self.GridColumnsReset();
                self.GridRowsReset();
            });
        }

        private void UpdateGridColumnsModel()
        {
            var cd = MainGrid.ColumnDefinitions;
            for (int i = 1; i < cd.Count; i++)
                Columns[i - 1].Size = (int)cd[i].Width.Value;
        }

        private void GridColumnsReset()
        {
            if (Columns.Count <= 1)
                return;

            double colSize = 100 / (Columns.Count - 1);

            var colDefs = MainGrid.ColumnDefinitions;
            for (int i = 1; i < colDefs.Count; i++)
            {
                var len = new GridLength(colSize, GridUnitType.Star);
                colDefs[i].Width = len;
                Columns[i - 1].Size = (int)len.Value * 10;
            }
        }

        private void UpdateGridRowsModel()
        {
            var rd = MainGrid.RowDefinitions;
            for (int i = 1; i < rd.Count; i++)
                Rows[i - 1].Size = (int)rd[i].Height.Value;
        }

        private void GridRowsReset()
        {
            if (Rows.Count <= 1)
                return;

            double rowSize = 100 / (Rows.Count - 1);

            var rowDefs = MainGrid.RowDefinitions;
            for (int i = 1; i < rowDefs.Count; i++)
            {
                var len = new GridLength(rowSize, GridUnitType.Star);
                rowDefs[i].Height = len;
                Rows[i - 1].Size = (int)len.Value * 10;
            }
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
            foreach (var it in Cards)
            {
                int hash = GetHashValue(it.ColumnDeterminant, it.RowDeterminant);

                if (!cells.ContainsKey(hash))
                    continue;

                cells[hash].Cards.Add(it);
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

    }//end of class
}
