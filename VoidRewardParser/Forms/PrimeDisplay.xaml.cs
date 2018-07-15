using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace VoidRewardParser.Forms
{
    /// <summary>
    /// Interaction logic for PrimeDisplay.xaml
    /// </summary>
    public partial class PrimeDisplay : UserControl
    {
        public PrimeDisplay()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }
    }
}