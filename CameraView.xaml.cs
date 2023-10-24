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
using EnvDTE90;

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
            capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
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
        }
        public void ThresholdDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.DetectionThreshold = (int)((Slider)sender).Value;
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

                Mat cameraImage = new Mat();
                
                using (cameraImage = capture.RetrieveMat())
                {
                    machine.currentRawImage = cameraImage.Clone();
                    if (machine.selectedCassette != null)
                    {
                        if (machine.selectedCassette.selectedFeeder != null)
                        {
                            if (machine.selectedCassette.selectedFeeder.part.TemplateFileName != null)
                            {
                                FindSelectedPartInImage(cameraImage);
                                OpenCvSharp.Cv2.DrawMarker(cameraImage, new OpenCvSharp.Point(machine.selectedCassette.selectedFeeder.x_next_part, machine.selectedCassette.selectedFeeder.y_next_part), new OpenCvSharp.Scalar(0, 0, 0), OpenCvSharp.MarkerTypes.Cross, 100, 1);

                            }
                        }
                    }
                    OpenCvSharp.Cv2.DrawMarker(cameraImage, new OpenCvSharp.Point(cameraImage.Cols/2, cameraImage.Rows/2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
               
                    Mat croppedImage = new Mat(cameraImage, ROI);
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

        private void FindSelectedPartInImage(Mat image)
        {
            Mat gref = new Mat();
            Mat gtpl = new Mat();
            Mat mres = new Mat();
            Mat res_32f;

            if(machine.selectedCassette.selectedFeeder.part.Template == null)
            {
                machine.selectedCassette.selectedFeeder.part.Template = new Mat(machine.selectedCassette.selectedFeeder.part.TemplateFileName);
            }
            Mat template = machine.selectedCassette.selectedFeeder.part.Template;

            Cv2.CvtColor(image, gref, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, gtpl, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            //const int low_canny = 12;  /* Lower Values capture more edges, default is 110*/
            //Cv2.Canny(gref, gref, low_canny, low_canny * 3);
            //Cv2.Canny(gtpl, gtpl, low_canny, low_canny * 3);

            /* Show gray images in window */
            //Cv2.ImShow("file", gref);
            //Cv2.ImShow("template", gtpl);

            res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(gref, gtpl, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(mres, MatType.CV_8U, 255.0);
            //Cv2.ImShow("result", mres);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(mres, mres, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold);
            //Cv2.ImShow("result_thresh", mres);

            /* Show all matches */
            while (true)
            {
                double minval, maxval;
                double bestval = 0;
                OpenCvSharp.Point minloc, maxloc, bestloc;
                Cv2.MinMaxLoc(mres, out minval, out maxval, out minloc, out maxloc);
                if (maxval > bestval)
                {
                    bestval = maxval;
                    bestloc = maxloc;
                    machine.selectedCassette.selectedFeeder.x_next_part = bestloc.X;
                    machine.selectedCassette.selectedFeeder.y_next_part = bestloc.Y;
                }


                if (maxval > 0)
                {
                    Cv2.Rectangle(image, maxloc, new OpenCvSharp.Point(maxloc.X + template.Cols, maxloc.Y + template.Rows), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(mres, maxloc, 0); //mark drawn blob
                }
                else
                    break;


            }

            

            /* Show Result */
            // Cv2.ImShow("final", machine.currentRawImage);
        }

        private void MachineView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
