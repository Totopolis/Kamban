using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kamban.MatrixControl
{
    /// <summary>
    /// Interaction logic for Intersection.xaml
    /// </summary>
    public partial class Intersection : UserControl
    {
        public Intersection()
        {
            InitializeComponent();

            Cards = new ReactiveList<ICard>();
        }

        public object ColumnDeterminant { get; set; }
        public object RowDeterminant { get; set; }

        public ReactiveList<ICard> Cards { get; set; }
    }//end of control
}
