using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System;
using System.Linq;
using System.Windows.Media.Imaging;
using EnvDTE90;
using System.Windows.Input;
using System.Windows.Media;
using System.Reflection;
using Xamarin.Forms.Shapes;
using System.Windows.Media.Media3D;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using TransformGroup = System.Windows.Media.TransformGroup;
using ScaleTransform = System.Windows.Media.ScaleTransform;
using Point = System.Windows.Point;
using System.Drawing.Imaging;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using static OpenCvSharp.ML.DTrees;

using Wpf.Ui.Appearance;
using Xamarin.Forms.PlatformConfiguration;
using Thread = System.Threading.Thread;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using System.Windows.Forms.VisualStyles;
using Xamarin.Forms;
using Slider = Xamarin.Forms.Slider;
using System.Security.Cryptography;

/* https://stackoverflow.com/questions/62573753/pan-and-zoom-but-contain-image-inside-parent-container */
/* https://stackoverflow.com/questions/741956/pan-zoom-image/6782715#6782715 */

namespace Picky
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    /// 
    

    public partial class CameraView : UserControl
    {
        private readonly CameraViewModel camera;
        private readonly VideoCapture capture;
        private readonly BackgroundWorker bkgWorker;
        MachineModel machine = MachineModel.Instance;

        private double x_next, y_next, y_last;
        System.Windows.Media.TransformGroup group;
        System.Windows.Media.ScaleTransform xform;
        System.Windows.Media.TranslateTransform tt;

        private Point start, origin;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public CameraView()
        {
            InitializeComponent();
            capture = new VideoCapture();

            camera = new CameraViewModel(capture);
            this.DataContext = camera;

            group = new System.Windows.Media.TransformGroup();
            xform = new System.Windows.Media.ScaleTransform();
            group.Children.Add(xform);
            tt = new System.Windows.Media.TranslateTransform();
            group.Children.Add(tt);

            FrameImage.RenderTransform = group;
            FrameImage.RenderTransformOrigin = new Point(0.0, 0.0);

            FrameImage.MouseWheel += Image_MouseWheel;
            FrameImage.MouseLeftButtonDown += Image_MouseDown;
            FrameImage.MouseLeftButtonUp += Image_MouseUp;
            FrameImage.MouseMove += Image_MouseMove;
            PreviewMouseRightButtonDown += delegate { Reset(); };

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
        }
   


        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameImage.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (FrameImage.IsMouseCaptured)
            {
                var st = GetScaleTransform(FrameImage);
                var tt = GetTranslateTransform(FrameImage);
                Vector v = start - e.GetPosition(this);
               
                /* Keep origin within frame */
                if ((origin.X - v.X) < 0)
                    tt.X = (origin.X - v.X);
                else
                    tt.X = 0;
                if((origin.Y - v.Y) < 0)
                    tt.Y = (origin.Y - v.Y);
                else
                    tt.Y = 0;
                /* Keep extents within frame */
                if ( ( -(origin.X - v.X) + FrameImage.ActualWidth) > (FrameImage.ActualWidth * st.ScaleX))
                    tt.X = FrameImage.ActualWidth - (FrameImage.ActualWidth * st.ScaleX);
                if ((-(origin.Y - v.Y) + FrameImage.ActualHeight) > (FrameImage.ActualHeight * st.ScaleY))
                    tt.Y = FrameImage.ActualHeight - (FrameImage.ActualHeight * st.ScaleY);
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var tt = GetTranslateTransform(FrameImage);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            this.Cursor = Cursors.Hand;
            FrameImage.CaptureMouse();
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var st = GetScaleTransform(FrameImage);
            var tt = GetTranslateTransform(FrameImage);

            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                return;

            Point relative = e.GetPosition(FrameImage);
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            if(st.ScaleX < 1)
                st.ScaleX = 1;
            if (st.ScaleY < 1)
                st.ScaleY = 1;
                      
            /* Keep origin within frame */
            if ((absoluteX - relative.X * st.ScaleX) < 0)
                tt.X = (absoluteX - relative.X * st.ScaleX);
            else
                tt.X = 0;
            if ((absoluteY - relative.Y * st.ScaleY) < 0)
                tt.Y = (absoluteY - relative.Y * st.ScaleY);
            else
                tt.Y = 0;
            /* Keep extents within frame */
            if ((-(absoluteX - relative.X * st.ScaleX) + FrameImage.ActualWidth) > (FrameImage.ActualWidth * st.ScaleX))
                tt.X = FrameImage.ActualWidth - (FrameImage.ActualWidth * st.ScaleX);
            if ((-(absoluteY - relative.Y * st.ScaleY) + FrameImage.ActualHeight) > (FrameImage.ActualHeight * st.ScaleY))
                tt.Y = FrameImage.ActualHeight - (FrameImage.ActualHeight * st.ScaleY);
        }

        private void Reset()
        {
            var st = GetScaleTransform(FrameImage);
            xform.ScaleX = 1.0;
            xform.ScaleY = 1.0;

            var tt = GetTranslateTransform(FrameImage);
            tt.X = 0.0;
            tt.Y = 0.0;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            capture.Open(0);
            capture.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            capture.Set(VideoCaptureProperties.BufferSize, 200);
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

        public void ThresholdDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.DetectionThreshold = (int)((Slider)sender).Value;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (camera.cameraImage = capture.RetrieveMat())
                {
                    machine.currentRawImage = camera.cameraImage.Clone();
                    if (machine?.selectedCassette?.selectedFeeder?.part?.TemplateFileName != null)
                    {
                        FindSelectedPartInImage(camera.cameraImage);
                        OpenCvSharp.Cv2.DrawMarker(camera.cameraImage, new OpenCvSharp.Point(x_next, y_next), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                    }
                    else
                    {
                        OpenCvSharp.Rect rect = new OpenCvSharp.Rect();
                        rect = FindRectangleInImage(camera.cameraImage);
                        
                        
                        Cv2.Rectangle(camera.cameraImage, rect, new OpenCvSharp.Scalar(255, 0, 255),2);
                    }
                    OpenCvSharp.Cv2.DrawMarker(camera.cameraImage, new OpenCvSharp.Point(camera.cameraImage.Cols/2, camera.cameraImage.Rows/2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                    Dispatcher.Invoke(() =>
                    {
                        if(camera.SelectedVisualizationViewItem.viewName.Equals("Normal View"))
                            FrameImage.Source = camera.cameraImage.ToWriteableBitmap();
                        else if (camera.SelectedVisualizationViewItem.viewName.Equals("Grayscale"))
                            FrameImage.Source = camera.grayImage.ToWriteableBitmap();
                        else if (camera.SelectedVisualizationViewItem.viewName.Equals("Threshold"))
                            FrameImage.Source = camera.thresImage.ToWriteableBitmap();
                        else if (camera.SelectedVisualizationViewItem.viewName.Equals("Edge Image"))
                            FrameImage.Source = camera.edgeImage.ToWriteableBitmap();
                    });

                }
                Thread.Sleep(10);
            }
        }

        private OpenCvSharp.Rect FindRectangleInImage(Mat srcImage) {

            OpenCvSharp.Rect calRect = new OpenCvSharp.Rect();
                                   
            // Convert the image to grayscale
            Cv2.CvtColor(srcImage, camera.grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(camera.grayImage, camera.grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(camera.grayImage, camera.thresImage, 100, 255, ThresholdTypes.Binary);
            
            // Use Canny edge detection to find edges in the image
            Cv2.Canny(camera.thresImage, camera.edgeImage, 50, 150, 3);
            
            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(camera.edgeImage, camera.dilatedImage, null, iterations: 2);

            // Find contours in the image
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(camera.dilatedImage, out OpenCvSharp.Point[][] contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);
            
            double largestArea = 0;
            OpenCvSharp.Rect largestRect = new OpenCvSharp.Rect(0,0,0,0);
           
            foreach (var contour in contours)
            {
                double area = Cv2.ContourArea(contour);
                if (area > 10000 && area > largestArea)
                {
                    largestArea = area;
                    largestRect = Cv2.BoundingRect(contour);
                }
            }

            Console.WriteLine("resolution-x: " + Constants.GetImageScaleAtDistanceX(machine.CurrentZ) + "mm/pix  Rect: " + largestRect.Width + " " + largestRect.Height);
            return largestRect;
        }

        
        

        private void FindSelectedPartInImage(Mat image)
        {
            Mat grayTemplateImage = new Mat();
            Mat matchResultImage = new Mat();
            Mat res_32f;

            if(machine.selectedCassette.selectedFeeder.part.Template == null)
            {
                machine.selectedCassette.selectedFeeder.part.Template = new Mat(machine.selectedCassette.selectedFeeder.part.TemplateFileName);
            }
            Mat template = machine.selectedCassette.selectedFeeder.part.Template;

            Cv2.CvtColor(image, camera.grayImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            //const int low_canny = 12;  /* Lower Values capture more edges, default is 110*/
            //Cv2.Canny(gref, gref, low_canny, low_canny * 3);
            //Cv2.Canny(gtpl, gtpl, low_canny, low_canny * 3);

            /* Show gray images in window */
            //Cv2.ImShow("file", gref);
            //Cv2.ImShow("template", gtpl);

            res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(camera.grayImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(matchResultImage, MatType.CV_8U, 255.0);
            //Cv2.ImShow("result", mres);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(matchResultImage, camera.thresImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold);
            //Cv2.ImShow("result_thresh", mres);

            y_last = Constants.CAMERA_FRAME_HEIGHT;
            /* Show all matches */
            while (true)
            {
                double minval, maxval;
                OpenCvSharp.Point minloc, maxloc;
                Cv2.MinMaxLoc(camera.thresImage, out minval, out maxval, out minloc, out maxloc);
                if (maxloc.Y > 0 && maxloc.X > 0 && maxloc.Y < y_last)
                {
                    y_last = maxloc.Y;
                    x_next = (maxloc.X + template.Cols / 2);
                    y_next = (maxloc.Y + template.Rows / 2);
                }

                if (maxval > 0)
                {
                    Cv2.Rectangle(image, maxloc, new OpenCvSharp.Point(maxloc.X + template.Cols, maxloc.Y + template.Rows), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(camera.thresImage, maxloc, 0); //mark drawn blob
                }
                else
                {
                    double x_offset_pixels = x_next - (0.5 * Constants.CAMERA_FRAME_WIDTH);
                    double y_offset_pixels = -(y_next - (0.5 * Constants.CAMERA_FRAME_HEIGHT));
                    machine.selectedCassette.selectedFeeder.x_next_part = machine.CurrentX + (x_offset_pixels * Constants.GetImageScaleAtDistanceX(machine.CurrentZ));
                    machine.selectedCassette.selectedFeeder.y_next_part = machine.CurrentY + (y_offset_pixels * Constants.GetImageScaleAtDistanceY(machine.CurrentZ));
                    break;
                }
            }
        }
    }
}
