using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Picky
{
    /// <summary>
    /// Interaction logic for PickView.xaml
    /// </summary>
    public partial class PickToolWindow : Window
    {
        PickToolViewModel pickToolVM;
        public PickToolWindow()
        {
            InitializeComponent();
            pickToolVM = new PickToolViewModel();
            this.DataContext = pickToolVM;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Hide the window instead of closing it
            this.Hide();
        }

        private void PickToolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PickListGrid.ScrollIntoView(PickListGrid.SelectedItem);
        }
    }
}