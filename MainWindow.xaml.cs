using System.ComponentModel;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using static Picky.MachineMessage;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
        private readonly SerialInterface serialInterface;

        private readonly ControlWindow controlWindow;
        private readonly PickListView pickListView;


        public MainWindow()
        {

            InitializeComponent();
            serialInterface = new SerialInterface();

            cassetteView = new CassetteView();
            cView.Children.Add(cassetteView);

            pickListView = new PickListView();
            lView.Children.Add(pickListView);

            controlWindow = new ControlWindow();
            DataContext = this;
        }
           

        public ICommand OnControlsCommand { get { return new RelayCommand(onControl); } }
        private void onControl()
        {
            controlWindow.Show();
        }

        public ICommand OnCalibrateCommand { get { return new RelayCommand(onCalibrate); } }
        private void onCalibrate()
        {
            controlWindow.calibrationWindow.Show();
        }
               
        public ICommand OnAssembleCommand { get { return new RelayCommand(onAssemble); } }
        private void onAssemble()
        {
            controlWindow.messageWindow.Show();
        }

    }
}
