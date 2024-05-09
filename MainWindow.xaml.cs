using System.ComponentModel;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using static Picky.MachineMessage;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

/********************************************************************
 * Notes:
 * 
 * For GUI See https://github.com/lepoco/wpfui
 * For S3G Serial Commands See: https://github.com/makerbot/s3g/blob/master/doc/s3gProtocol.md
 * 
 *********************************************************************/

namespace Picky
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
               
        private readonly CassetteView cassetteView;
        private readonly MachineView machineView;
        private readonly ControlView controlsView;
        private readonly CameraView cameraView;
        private readonly SerialInterface serialInterface;

        private readonly ControlWindow controlWindow;
        private readonly PickListView pickListView;


        public MainWindow()
        {
            
            InitializeComponent();
            serialInterface = new SerialInterface();
           
            //machineView = new MachineView();
            //mView.Children.Add(machineView);
            
            cassetteView = new CassetteView();
            cView.Children.Add(cassetteView);

            pickListView = new PickListView();
            lView.Children.Add(pickListView);

            //controlsView = new ControlView();
            //ctrlView.Children.Add(controlsView);

            //cameraView = new CameraView();
            //camView.Children.Add(cameraView);

            controlWindow = new ControlWindow();
            controlWindow.Show();

        }
    }
}
