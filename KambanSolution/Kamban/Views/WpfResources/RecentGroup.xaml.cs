using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DynamicData;
using Kamban.ViewModels.Core;
using ReactiveUI;

namespace Kamban.Views.WpfResources
{
    /// <summary>
    /// Interaction logic for RecentGroup.xaml
    /// </summary>
    public partial class RecentGroup : UserControl
    {
        public RecentGroup()
        {
            InitializeComponent();
        }

        public ReadOnlyObservableCollection<RecentViewModel> Recent
        {
            get => (ReadOnlyObservableCollection<RecentViewModel>)GetValue(RecentProperty);
            private set => SetValue(RecentProperty, value);
        }

        public static readonly DependencyProperty RecentProperty =
            DependencyProperty.Register("Recent",
                typeof(ReadOnlyObservableCollection<RecentViewModel>),
                typeof(RecentGroup),
                new PropertyMetadata(null));

        public ReactiveCommand<RecentViewModel, Unit> OpenRecentCommand
        {
            get => (ReactiveCommand<RecentViewModel, Unit>)GetValue(OpenRecentCommandProperty);
            set => SetValue(OpenRecentCommandProperty, value);
        }

        public static readonly DependencyProperty OpenRecentCommandProperty =
            DependencyProperty.Register("OpenRecentCommand",
                typeof(ReactiveCommand<RecentViewModel, Unit>),
                typeof(RecentGroup),
                new PropertyMetadata(null));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header",
                typeof(string),
                typeof(RecentGroup),
                new PropertyMetadata(null));

        public IObservable<IChangeSet<RecentViewModel>> RecentObservable
        {
            get => (IObservable<IChangeSet<RecentViewModel>>)GetValue(RecentObservableProperty);
            set => SetValue(RecentObservableProperty, value);
        }

        public static readonly DependencyProperty RecentObservableProperty =
            DependencyProperty.Register("RecentObservable",
                typeof(IObservable<IChangeSet<RecentViewModel>>),
                typeof(RecentGroup),
                new PropertyMetadata(null, OnRecentObservableProperty));

        public static void OnRecentObservableProperty(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var rg = obj as RecentGroup;

            if (rg.RecentObservable == null)
                return;

            rg.RecentObservable
                .Bind(out ReadOnlyObservableCollection<RecentViewModel> temp)
                .Subscribe();

            rg.Recent = temp;

            rg.RecentObservable
              .Subscribe(x => rg.Height = rg.Recent.Count > 0 ? double.NaN : 0);
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            var button = sender as Button;

            var cb = FindChild<CheckBox>(button, "PinCheck");
            if (cb != null)
                cb.Visibility = Visibility.Visible;
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            var button = sender as Button;

            var cb = FindChild<CheckBox>(button, "PinCheck");
            if (cb != null)
                cb.Visibility = Visibility.Hidden;
        }

        public static T FindChild<T>(DependencyObject depObj, string childName)
            where T : DependencyObject
        {
            // Confirm obj is valid. 
            if (depObj == null) return null;

            // success case
            if (depObj is T && ((FrameworkElement)depObj).Name == childName)
                return depObj as T;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                //DFS
                T obj = FindChild<T>(child, childName);

                if (obj != null)
                    return obj;
            }

            return null;
        }
    }//end of class
}
