﻿using Kamban.ViewModels;
using wpf.ui;

namespace Kamban.Views
{
    public partial class WizardView : IView
    {
        public WizardView(WizardViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        public IViewModel ViewModel { get; set; }

        public void Configure(UiShowOptions options)
        {
            ViewModel.Title = options.Title;
        }
    }
}
