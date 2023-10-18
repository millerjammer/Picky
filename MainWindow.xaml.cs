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
        private readonly VideoCapture capture;
        private readonly BackgroundWorker bkgWorker;

        public MachineModel machineModel;
        private readonly CassetteView cassetteView;
        private readonly MachineView machineView;


        public MainWindow()
        {
            
            InitializeComponent();
            capture = new VideoCapture();
           
            machineModel = new MachineModel();
            
            cassetteView = new CassetteView(machineModel);
            cView.Children.Add(cassetteView);

            machineView = new MachineView(machineModel);
            mView.Children.Add(machineView);

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

        }
        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            //capture.Set(VideoCaptureProperties.FrameWidth, 1920);
            //capture.Set(VideoCaptureProperties.FrameHeight, 1080);
            // 5Mpix Settings
            capture.Set(VideoCaptureProperties.FrameWidth, 2592);
            capture.Set(VideoCaptureProperties.FrameHeight, 1944);

            capture.Open(0);
            if (!capture.IsOpened())
            {
                Close();
                return;
            }
            //double maxHeight = capture.Get(VideoCaptureProperties.FrameHeight);
            //double maxWidth = capture.Get(VideoCaptureProperties.FrameWidth);
            //Console.Write("here: " + maxHeight + " " +maxWidth + "\n");

            bkgWorker.RunWorkerAsync();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bkgWorker.CancelAsync();
            capture.Dispose();
            
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }
                Thread.Sleep(30);
            }
        }

        private void MachineView_Loaded(object sender, RoutedEventArgs e)
        {

        }

    /*    void ToggleLEDEvent(object sender, EventArgs e)
        {
            if ((bool)toggleLED.IsChecked)
                relayInterface.SetIlluminatorOn(true);
            else
                relayInterface.SetIlluminatorOn(false);

        }

        void TogglePumpEvent(object sender, EventArgs e)
        {
            if ((bool)togglePump.IsChecked)
                relayInterface.SetPumpOn(true);
            else
                relayInterface.SetPumpOn(false);

        }

        

        void OnButtonStop(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_EnableSteppers((byte)(Constants.A_AXIS | Constants.B_AXIS | Constants.X_AXIS | Constants.Y_AXIS | Constants.Z_AXIS), false));
        }

        void OnHome(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_Initialize());
            machine.messages.Enqueue(MachineCommands.S3G_FindXYMaximums());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
            machine.messages.Enqueue(MachineCommands.S3G_FindZMinimum());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
            machine.messages.Enqueue(MachineCommands.S3G_SetPositionAs(0, 0, 0, 0, 0));
            machine.messages.Enqueue(MachineCommands.S3G_SetAbsoluteAngle(0));
            machine.messages.Enqueue(MachineCommands.S3G_SetAbsoluteXYPosition(-50, -50));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }
    */
    }
}
