using System.Windows.Controls;

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
