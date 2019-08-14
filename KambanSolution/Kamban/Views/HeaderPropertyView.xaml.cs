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
using System.Windows.Shapes;
using System.Text.RegularExpressions;


namespace Kamban.Views
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class HeaderPropertyView 
    {
        public HeaderPropertyView()
        
        {
            InitializeComponent();
        }

   //     public string HeaderName { get; set; }


       private void update_Bindings()
        {
            TB_HeaderName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
}
}
