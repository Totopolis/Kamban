﻿using System;
using System.Linq;
using Kamban.ViewModels;
using wpf.ui;

namespace Kamban.Views
{
    public partial class BoardEditForExportView : IView, IStretchedSizeView
    {
        public BoardEditForExportView(BoardEditForExportViewModel localBoardViewModel)
        {
            InitializeComponent();
            ViewModel = localBoardViewModel;
            DataContext = ViewModel;
        }

        public IViewModel ViewModel { get; set; }

        public void Configure(UiShowOptions options)
        {
            ViewModel.FullTitle = options.Title;
        }

        public double StretchedWidth
        {
            get
            {
                var starWidthTotal = Matrix.MainGrid.ColumnDefinitions.Skip(1)
                    .Select(x => x.Width.Value)
                    .Sum();
                var maxWidth = Matrix.MainGrid.ColumnDefinitions.Skip(1)
                    .Max(x => Math.Ceiling(x.ActualWidth * starWidthTotal / x.Width.Value));
                maxWidth += Matrix.MainGrid.ColumnDefinitions[0].ActualWidth;
                return maxWidth;
            }
        }

        public double StretchedHeight
        {
            get
            {
                var starHeightTotal = Matrix.MainGrid.RowDefinitions.Skip(1)
                    .Select(x => x.Height.Value)
                    .Sum();
                var maxHeight = Matrix.MainGrid.RowDefinitions.Skip(1)
                    .Max(rd => Math.Ceiling(rd.ActualHeight * starHeightTotal / rd.Height.Value));
                maxHeight += Matrix.MainGrid.RowDefinitions[0].ActualHeight;
                return maxHeight;
            }
        }
    }
}
