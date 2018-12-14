using System;
using System.Windows;
using PdfSharp;

namespace Kamban.Core
{
    [Flags]
    public enum ScaleFitting
    {
        None = 0,
        Vertical = 1,
        Horizontal = 2,
        BothDirections = 3
    }

    public class ScaleOptions
    {
        public ScaleFitting ScaleFitting { get; set; }
        public bool ScaleToFit { get; set; }
        public double MinScale { get; set; }
        public double MaxScale { get; set; }
        public Thickness Padding { get; set; }
    }

    public class PdfOptionsAvailable
    {
        public PageSize[] PageSizes { get; set; }
        public PageOrientation[] PageOrientations { get; set; }
        public ScaleFitting[] ScaleFittings { get; set; }
    }

    public class PdfOptions
    {
        public PageSize PageSize { get; set; }
        public PageOrientation PageOrientation { get; set; }
        public ScaleOptions ScaleOptions { get; set; }
    }
}
