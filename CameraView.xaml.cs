using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System;
using System.Windows.Media.Imaging;

namespace Picky
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    public partial class CameraView : UserControl
    {
        private readonly CameraViewModel camera;
        private readonly VideoCapture capture;
        private readonly BackgroundWorker bkgWorker;
        MachineModel machine = MachineModel.Instance;

        public CameraView()
        {
            InitializeComponent();
            capture = new VideoCapture();
            
            camera = new CameraViewModel(capture);
            this.DataContext = camera;

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            capture.Open(0);
            capture.Set(VideoCaptureProperties.FrameWidth, 1280);
            capture.Set(VideoCaptureProperties.FrameHeight, 960);
           
            if (!capture.IsOpened())
            {
                //Close();
                return;
            }

            bkgWorker.RunWorkerAsync();
        }

        public void Dispose()
        {
            bkgWorker.CancelAsync();
            capture.Dispose();

        }

        public void FocusDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.Focus = (int)((Slider)sender).Value;
        }

        public void ZoomDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.Zoom = (int)((Slider)sender).Value;
            Console.WriteLine("zoom " + camera.Zoom);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                int imgWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                int imgHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);

                int width = imgWidth / camera.Zoom;
                int height = imgHeight / camera.Zoom;
                int x = (imgWidth - width) / 2;
                int y = (imgHeight - height) / 2;
                OpenCvSharp.Rect ROI = new OpenCvSharp.Rect(x, y, width, height);
                OpenCvSharp.Size Size = new OpenCvSharp.Size(imgWidth, imgHeight);

                Mat temp = new Mat();
                using ( temp = capture.RetrieveMat())
                {
                    machine.currentRawImage = temp.Clone();
                    Mat croppedImage = new Mat(machine.currentRawImage, ROI);
                    OpenCvSharp.Cv2.Resize(croppedImage, croppedImage, Size);
                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = croppedImage.ToWriteableBitmap();
                    });

                }
                Thread.Sleep(100);
            }
        }

        private void MachineView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
