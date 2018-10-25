using DynamicData;
using GongSolutions.Wpf.DragDrop;
using Monik.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Legacy;
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
        public Matrix()
        {
            InitializeComponent();
        }

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

        public IMonik Monik
        {
            get => (IMonik)GetValue(MonikProperty);
            set => SetValue(MonikProperty, value);
        }

        public static readonly DependencyProperty MonikProperty =
            DependencyProperty.Register("Monik",
                typeof(IMonik),
                typeof(Matrix),
                new PropertyMetadata(null));

        public SourceList<ICard> Cards
        {
            get => (SourceList<ICard>)GetValue(CardsProperty);
            set => SetValue(CardsProperty, value);
        }

        public static readonly DependencyProperty CardsProperty =
            DependencyProperty.Register("Cards",
                typeof(SourceList<ICard>),
                typeof(Matrix),
                new PropertyMetadata(new SourceList<ICard>(),
                    new PropertyChangedCallback(OnCardsPropertyChanged)));

        public SourceList<IDim> Columns
        {
            get => (SourceList<IDim>)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns",
                typeof(SourceList<IDim>),
                typeof(Matrix),
                new PropertyMetadata(new SourceList<IDim>(),
                    new PropertyChangedCallback(OnColumnsPropertyChanged)));

        public SourceList<IDim> Rows
        {
            get => (SourceList<IDim>)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows",
                typeof(SourceList<IDim>),
                typeof(Matrix),
                new PropertyMetadata(new SourceList<IDim>(),
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
