using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Kamban.Common
{
    public class ColorItem
    {
        public SolidColorBrush Brush { get; private set; }
        public string Name { get; private set; }
        public string SystemName => Brush.Color.ToString();

        private static readonly Dictionary<string, ColorItem> Colors = new Dictionary<string, ColorItem>();

        internal static ColorItem Create(string colorName)
        {
            if (Colors.ContainsKey(colorName))
                return Colors[colorName];

            var converter = new ColorConverter();

            Color color;
            try
            {
                color = (Color) converter.ConvertFromInvariantString(colorName);
            }
            catch (NullReferenceException e)
            {
                throw new ArgumentException($"Cannot convert to color {colorName}", e);
            }

            var item = new ColorItem
            {
                Brush = new SolidColorBrush(color),
                Name = colorName
            };

            Colors.Add(colorName, item);

            return item;
        }

        public static string ToColorName(string systemName)
        {
            return Colors.Values
                .FirstOrDefault(x => x.SystemName == systemName)?
                .Name;
        }
    }
}