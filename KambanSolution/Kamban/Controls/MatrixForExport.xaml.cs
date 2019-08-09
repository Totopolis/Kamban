using System.Windows;
using System.Windows.Controls;
using Kamban.ViewModels.Core;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Matrix For Export to PDF and Printing
    /// </summary>
    public partial class MatrixForExport : UserControl
    {
        public MatrixForExport()
        {
            InitializeComponent();
        }

        public bool ShowCardIds
        {
            get => (bool)GetValue(ShowCardIdsProperty);
            set => SetValue(ShowCardIdsProperty, value);
        }

        public static readonly DependencyProperty ShowCardIdsProperty =
            DependencyProperty.Register("ShowCardIds",
                typeof(bool),
                typeof(MatrixForExport),
                new PropertyMetadata(false));

        public static void OnEnableWorkPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mx = (MatrixForExport)obj;
            
            if (mx.EnableWork)
                mx.RebuildGrid();
        }

        public bool EnableWork
        {
            get => (bool)GetValue(EnableWorkProperty);
            set => SetValue(EnableWorkProperty, value);
        }

        public static readonly DependencyProperty EnableWorkProperty =
            DependencyProperty.Register("EnableWork",
                typeof(bool),
                typeof(MatrixForExport),
                new PropertyMetadata(false, OnEnableWorkPropertyChanged));

        public ICard[] Cards
        {
            get => (ICard[])GetValue(CardsProperty);
            set => SetValue(CardsProperty, value);
        }

        public static readonly DependencyProperty CardsProperty =
            DependencyProperty.Register("Cards",
                typeof(ICard[]),
                typeof(MatrixForExport),
                new PropertyMetadata(null));

        public ColumnViewModel[] Columns
        {
            get => (ColumnViewModel[])GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns",
                typeof(ColumnViewModel[]),
                typeof(MatrixForExport),
                new PropertyMetadata(null));

        public RowViewModel[] Rows
        {
            get => (RowViewModel[])GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows",
                typeof(RowViewModel[]),
                typeof(MatrixForExport),
                new PropertyMetadata(null));
        
    }//end of class
}
