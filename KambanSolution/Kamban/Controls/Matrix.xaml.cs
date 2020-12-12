using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GongSolutions.Wpf.DragDrop;
using Kamban.ViewModels.Core;
using ReactiveUI;
using Serilog;
using CardsObservableType = System.IObservable<DynamicData.IChangeSet<Kamban.ViewModels.Core.ICard>>;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Kanban.xaml
    /// </summary>
    public partial class Matrix : UserControl, IDropTarget
    {
        public Matrix()
        {
            InitializeComponent();

            PropertyDescriptor pdSL = DependencyPropertyDescriptor.FromProperty(Matrix.SwimLaneViewProperty, typeof(Matrix));
            pdSL.AddValueChanged(this, new System.EventHandler(SwimLanePropertyChanged) );

            PropertyDescriptor pdColorTheme = DependencyPropertyDescriptor.FromProperty(Matrix.ColorThemeProperty, typeof(Matrix));
            pdColorTheme.AddValueChanged(this, new System.EventHandler(ColorThemePropertyChanged));
        }

        public bool ShowCardIds
        {
            get => (bool)GetValue(ShowCardIdsProperty);
            set => SetValue(ShowCardIdsProperty, value);
        }

        public static readonly DependencyProperty ShowCardIdsProperty =
            DependencyProperty.Register("ShowCardIds",
                typeof(bool),
                typeof(Matrix),
                new PropertyMetadata(false));

        public bool SwimLaneView
        {
            get => (bool)GetValue(SwimLaneViewProperty);
            set => SetValue(SwimLaneViewProperty, value);
        }

        public static readonly DependencyProperty SwimLaneViewProperty =
            DependencyProperty.Register("SwimLaneView",
                typeof(bool),
                typeof(Matrix),
                new PropertyMetadata(false));

        public bool EnableWork
        {
            get => (bool)GetValue(EnableWorkProperty);
            set => SetValue(EnableWorkProperty, value);
        }

        public static readonly DependencyProperty EnableWorkProperty =
            DependencyProperty.Register("EnableWork",
                typeof(bool),
                typeof(Matrix),
                new PropertyMetadata(false, new PropertyChangedCallback(OnEnableWorkPropertyChanged)));


        public Color ColorTheme
        {
            get => (Color)GetValue(ColorThemeProperty);
            set => SetValue(ColorThemeProperty, value);
        }

        public static readonly DependencyProperty ColorThemeProperty =
            DependencyProperty.Register("ColorTheme",
                typeof(Color),
                typeof(Matrix),
                new PropertyMetadata(Color.FromArgb(0, 255, 255, 255))); // transparent

        public ILogger Monik
        {
            get => (ILogger)GetValue(MonikProperty);
            set => SetValue(MonikProperty, value);
        }

        public static readonly DependencyProperty MonikProperty =
            DependencyProperty.Register("Monik",
                typeof(ILogger),
                typeof(Matrix),
                new PropertyMetadata(null));

        public CardsObservableType CardsObservable
        {
            get => (CardsObservableType)GetValue(CardsObservableProperty);
            set => SetValue(CardsObservableProperty, value);
        }

        public static readonly DependencyProperty CardsObservableProperty =
            DependencyProperty.Register("CardsObservable",
                typeof(CardsObservableType),
                typeof(Matrix),
                new PropertyMetadata(null,
                    new PropertyChangedCallback(OnCardsObservablePropertyChanged)));

        public ReadOnlyObservableCollection<ColumnViewModel> Columns
        {
            get => (ReadOnlyObservableCollection<ColumnViewModel>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns",
                typeof(ReadOnlyObservableCollection<ColumnViewModel>),
                typeof(Matrix),
                new PropertyMetadata(null, new PropertyChangedCallback(OnColumnsPropertyChanged)));

        public ReadOnlyObservableCollection<RowViewModel> Rows
        {
            get => (ReadOnlyObservableCollection<RowViewModel>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows",
                typeof(ReadOnlyObservableCollection<RowViewModel>),
                typeof(Matrix),
                new PropertyMetadata(null, new PropertyChangedCallback(OnRowsPropertyChanged)));

        public ICard CardUnderMouse
        {
            get => (ICard)GetValue(CardUnderMouseProperty);
            set => SetValue(CardUnderMouseProperty, value);
        }

        public static readonly DependencyProperty CardUnderMouseProperty =
            DependencyProperty.Register("CardUnderMouse",
                typeof(ICard),
                typeof(Matrix),
                new PropertyMetadata(null));

        public ICard CardOfContextMenu
        {
            get => (ICard)GetValue(CardOfContextMenuProperty);
            set => SetValue(CardOfContextMenuProperty, value);
        }

        public static readonly DependencyProperty CardOfContextMenuProperty =
            DependencyProperty.Register("CardOfContextMenu",
                typeof(ICard),
                typeof(Matrix),
                new PropertyMetadata(null));

        public ReactiveCommand<ICard, Unit> CardClickCommand
        {
            get => (ReactiveCommand<ICard, Unit>)GetValue(CardClickCommandProperty);
            set => SetValue(CardClickCommandProperty, value);
        }

        public static readonly DependencyProperty CardClickCommandProperty =
            DependencyProperty.Register(
                "CardClickCommand",
                typeof(ReactiveCommand<ICard, Unit>),
                typeof(Matrix),
                new PropertyMetadata(null));

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

        public ContextMenu CardContextMenu
        {
            get => (ContextMenu)GetValue(CardContextMenuProperty);
            set => SetValue(CardContextMenuProperty, value);
        }

        public static readonly DependencyProperty CardContextMenuProperty =
            DependencyProperty.Register(
                "CardContextMenu",
                typeof(ContextMenu),
                typeof(Matrix),
                new PropertyMetadata(null));

        public ContextMenu HeadContextMenu
        {
            get => (ContextMenu)GetValue(HeadContextMenuProperty);
            set => SetValue(HeadContextMenuProperty, value);
        }

        public static readonly DependencyProperty HeadContextMenuProperty =
            DependencyProperty.Register(
                "HeadContextMenu",
                typeof(ContextMenu),
                typeof(Matrix),
                new PropertyMetadata(null));

        public object HeadOfContextMenu
        {
            get => (object)GetValue(HeadOfContextMenuProperty);
            set => SetValue(HeadOfContextMenuProperty, value);
        }

        public static readonly DependencyProperty HeadOfContextMenuProperty =
            DependencyProperty.Register("HeadOfContextMenu",
                typeof(object),
                typeof(Matrix),
                new PropertyMetadata(null));

        public ReactiveCommand<(object column, object row), Unit> CellDoubleClickCommand
        {
            get => (ReactiveCommand<(object column, object row), Unit>)GetValue(CellDoubleClickCommandProperty);
            set => SetValue(CellDoubleClickCommandProperty, value);
        }

        public static readonly DependencyProperty CellDoubleClickCommandProperty =
            DependencyProperty.Register(
                "CellDoubleClickCommand",
                typeof(ReactiveCommand<(object column, object row), Unit>),
                typeof(Matrix),
                new PropertyMetadata(null));

    }//end of class
}
