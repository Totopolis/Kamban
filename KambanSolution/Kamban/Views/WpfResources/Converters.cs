using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Kamban.ViewModels;
using ReactiveUI;

namespace Kamban.Views.WpfResources
{
    public abstract class BaseConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public abstract object Convert(object value, Type targetType, object parameter,
                                       CultureInfo culture);


        public virtual object ConvertBack(object value, Type targetType, object parameter,
                                          CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MoreThenOneToVisibility : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter,
                                       CultureInfo culture)
        {
            return (int?) value > 1 ? Visibility.Visible : Visibility.Hidden;
        } 
    } 

    public class BoolInverse : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter,
                                       CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public class ColorNameToSolidColorBrushValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string colorName = value as string;

            if (colorName==null)
                return null;

            var brush = ColorItem.I(colorName);
            return brush.Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If necessary, here you can convert back. Check if which brush it is (if its one),
            // get its Color-value and return it.

            throw new NotImplementedException();
        }
    }
}
