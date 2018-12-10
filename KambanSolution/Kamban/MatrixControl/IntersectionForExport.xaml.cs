using System.Windows.Controls;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Intersection.xaml
    /// </summary>
    public partial class IntersectionForExport : UserControl
    {
        public IntersectionForExport()
        {
            InitializeComponent();
        }

        public ICard[] SelfCards { get; set; }
    }//end of control
}
