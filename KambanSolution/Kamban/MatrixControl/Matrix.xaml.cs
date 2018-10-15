using GongSolutions.Wpf.DragDrop;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Kanban.xaml
    /// </summary>
    public partial class Matrix : UserControl, IDropTarget
    {
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

        
        public ReactiveCommand<ICard, Unit> DropCardCommand
        {
            get => (ReactiveCommand<ICard, Unit>)GetValue(DropCardCommandProperty);
            set => SetValue(DropCardCommandProperty, value);
        }

        public static readonly DependencyProperty DropCardCommandProperty =
            DependencyProperty.Register(
                "DropCardCommand",
                typeof(ReactiveCommand<ICard, Unit>),
                typeof(Matrix),
                new PropertyMetadata(null));

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

    }//end of class
}
