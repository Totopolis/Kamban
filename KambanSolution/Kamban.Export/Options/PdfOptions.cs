using PdfSharp;

namespace Kamban.Export.Options
{
    public class PdfOptions
    {
        public PageSize PageSize { get; set; }
        public PageOrientation PageOrientation { get; set; }
        public ScaleOptions ScaleOptions { get; set; }
    }
}