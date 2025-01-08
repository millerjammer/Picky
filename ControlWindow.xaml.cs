using Microsoft.VisualStudio.Shell.Interop;
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
    /// Interaction logic for ControlWindow.xaml
    /// </summary>
    public partial class ControlWindow : Window
    {
        public CalibrationWindow calibrationWindow;
        public MessageWindow messageWindow;

        public ControlWindow()
        {
            InitializeComponent();

            MachineModel machine = MachineModel.Instance;
            upCam.Content = new CameraView(machine.upCamera);
            downCam.Content = new CameraView(machine.downCamera);

            calibrationWindow = new CalibrationWindow();
            messageWindow = new MessageWindow(machine);
           
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Hide the window instead of closing it
            this.Hide();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
