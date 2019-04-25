using System.Windows;

namespace Kamban.Export.Options
{
    public class ScaleOptions
    {
        public ScaleFitting ScaleFitting { get; set; }
        public bool ScaleToFit { get; set; }
        public double MinScale { get; set; }
        public double MaxScale { get; set; }
        public Thickness Padding { get; set; }
    }
}