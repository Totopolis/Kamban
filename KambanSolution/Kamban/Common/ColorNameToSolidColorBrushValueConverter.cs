using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Kamban.Common
{
    public class ColorNameToSolidColorBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string colorName = value as string;

            if (colorName == null)
                return null;

            var brush = ColorItem.Create(colorName);
            return brush.Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            // If necessary, here you can convert back. Check if which brush it is (if its one),
            // get its Color-value and return it.

            throw new NotImplementedException();
        }
    }

    public class ColorToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Color))
                throw new InvalidOperationException("Value must be a Color");
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
