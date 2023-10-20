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
        
        private readonly CassetteView cassetteView;
        private readonly MachineView machineView;
        private readonly ControlView controlsView;
        private readonly SerialInterface serialInterface;


        public MainWindow()
        {
            
            InitializeComponent();
            capture = new VideoCapture();
            serialInterface = new SerialInterface();
           
            machineView = new MachineView();
            mView.Children.Add(machineView);
            
            cassetteView = new CassetteView();
            cView.Children.Add(cassetteView);

            controlsView = new ControlView();
            ctrlView.Children.Add(controlsView);

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
    }
}
