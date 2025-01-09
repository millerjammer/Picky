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
using System.Windows.Shapes;

namespace Picky
{
    /// <summary>
    /// Interaction logic for FeederWindow.xaml
    /// </summary>
    public partial class FeederWindow : Window
    {
        FeederViewModel feederVM;
        public FeederWindow()
        {
            InitializeComponent();
            feederVM = new FeederViewModel();
            MachineModel machine = MachineModel.Instance;
            this.DataContext = machine;
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
