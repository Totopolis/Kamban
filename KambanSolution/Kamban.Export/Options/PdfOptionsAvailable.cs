using PdfSharp;

namespace Kamban.Export.Options
{
    public class PdfOptionsAvailable
    {
        public PageSize[] PageSizes { get; set; }
        public PageOrientation[] PageOrientations { get; set; }
        public ScaleFitting[] ScaleFittings { get; set; }
    }
}