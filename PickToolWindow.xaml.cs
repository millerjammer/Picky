using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Picky
{
    /// <summary>
    /// Interaction logic for PickView.xaml
    /// </summary>
    public partial class PickToolWindow : Window
    {
        PickToolViewModel pickVM;

        public PickToolWindow(MachineModel mm)
        {
            InitializeComponent();
            pickVM = new PickToolViewModel(mm);
            this.DataContext = pickVM;
        }

        private void PickToolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PickListGrid.ScrollIntoView(PickListGrid.SelectedItem);
        }



        protected override void OnClosing(CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Hide the window instead of closing it
            this.Hide();
        }
    }
}
