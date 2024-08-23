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
               
        private readonly CassetteView       cassetteView;
        private readonly SerialInterface    serialInterface;
        private readonly PickListView       pickListView;
        private readonly PickToolWindow     toolWindow;
        private readonly ControlWindow      controlWindow;
        
        public MainWindow()
        {

            InitializeComponent();
            serialInterface = new SerialInterface();

            cassetteView = new CassetteView();
            cView.Children.Add(cassetteView);

            pickListView = new PickListView();
            lView.Children.Add(pickListView);

            toolWindow = new PickToolWindow();
            DataContext = this;

            controlWindow = new ControlWindow();
            DataContext = this;
        }


        public ICommand OnToolsCommand { get { return new RelayCommand(onTools); } }
        private void onTools()
        {
            toolWindow.Show();
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
