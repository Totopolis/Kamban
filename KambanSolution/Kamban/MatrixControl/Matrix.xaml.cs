using ReactiveUI;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Ui.Wpf.KanbanControl;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Kanban.xaml
    /// </summary>
    public partial class Matrix : UserControl
    {
        public Matrix()
        {
            InitializeComponent();
        }

        public ReactiveList<object> Cards
        {
            get => (ReactiveList<object>)GetValue(CardsProperty);
            set => SetValue(CardsProperty, value);
        }

        public static readonly DependencyProperty CardsProperty =
            DependencyProperty.Register("Cards",
                typeof(IEnumerable),
                typeof(Matrix),
                new PropertyMetadata(new ReactiveList<object>()));

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

        private static readonly DefaultTemplates defaultTemplates = new DefaultTemplates();

        public void RebuildGrid()
        {
            MainGrid.Children.Clear();

            MainGrid.ColumnDefinitions.Clear();
            // rows header
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto});
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
