using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System;
using System.Linq;
using System.Windows.Input;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using TransformGroup = System.Windows.Media.TransformGroup;
using ScaleTransform = System.Windows.Media.ScaleTransform;
using Point = System.Windows.Point;
using Slider = Xamarin.Forms.Slider;
using EnvDTE80;
using Newtonsoft.Json.Linq;


/* https://stackoverflow.com/questions/62573753/pan-and-zoom-but-contain-image-inside-parent-container */
/* https://stackoverflow.com/questions/741956/pan-zoom-image/6782715#6782715 */
/* https://www.kurokesu.com/main/2020/07/12/pulling-full-resolution-from-a-webcam-with-opencv-windows/ */
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

        System.Windows.Media.TransformGroup group;
        System.Windows.Media.ScaleTransform xform;
        System.Windows.Media.TranslateTransform tt;

        private Point start, origin;
        private double x_part_cursor, y_part_cursor;


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
            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));

            
            capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            capture.Set(VideoCaptureProperties.AutoExposure, 1);
            capture.Set(VideoCaptureProperties.Brightness, .2);
            capture.Set(VideoCaptureProperties.BufferSize, 200);
            camera.IsManualFocus = false;
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
                    try
                    {
                        machine.currentRawImage = camera.cameraImage.Clone();
                    }catch (Exception ex)
                    {
                        Console.WriteLine("Ouchy - " + ex.ToString());
                    }

                    if(machine.CameraCalibrationState == MachineModel.CalibrationState.InProcess)
                    {
                        machine.CalRectangle = FindRectangleInImage(camera.cameraImage);
                        Cv2.Rectangle(camera.cameraImage, machine.CalRectangle, new OpenCvSharp.Scalar(255, 0, 255),2);
                    }
                    else if(machine.PickCalibrationState == MachineModel.CalibrationState.InProcess)
                    {
                        camera.IsManualFocus = true;
                        camera.Focus = Constants.CALIBRATION_PICK_FOCUS;
                        machine.CalRectangle = FindPickInImage(camera.cameraImage);
                        Cv2.Rectangle(camera.cameraImage, machine.CalRectangle, new OpenCvSharp.Scalar(0, 255, 0), 2);
                        double x_center = (machine.CalRectangle.X + (machine.CalRectangle.Width / 2));
                        double x_offset = x_center - (Constants.CAMERA_FRAME_WIDTH / 2);
                        double y_end = machine.CalRectangle.Height;
                        double y_offset = (Constants.CAMERA_FRAME_HEIGHT / 2) - y_end;
                        /* x_offset and y_offset are measured from center of the frame, in pixels. x and y are generally positive. Actual location in quadrant 1  */
                        OpenCvSharp.Cv2.DrawMarker(camera.cameraImage, new OpenCvSharp.Point(x_center, y_end), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                        Console.WriteLine("Pick @ Angle: " + (machine.CurrentB * Constants.B_DEGREES_PER_MM) + "d " + x_offset + "px " + (x_offset * (machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z))) + "mm " + y_offset + "px " + (y_offset * (machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z))) + "mm ");
                        if (machine.CalPick.x_min > x_offset)
                        {
                            machine.CalPick.x_min = x_offset;
                            machine.CalPick.x_min_angle = (machine.CurrentB * Constants.B_DEGREES_PER_MM);
                        }
                        if (machine.CalPick.x_max < x_offset)
                        {
                            machine.CalPick.x_max = x_offset;
                            machine.CalPick.x_max_angle = (machine.CurrentB * Constants.B_DEGREES_PER_MM);
                        }

                        if (machine.CalPick.h_min > y_offset)
                        {
                            machine.CalPick.h_min = y_offset;
                            machine.CalPick.h_min_angle = (machine.CurrentB * Constants.B_DEGREES_PER_MM);
                        }
                        if (machine.CalPick.h_max < y_offset)
                        {
                            machine.CalPick.h_max = y_offset;
                            machine.CalPick.h_max_angle = (machine.CurrentB * Constants.B_DEGREES_PER_MM);
                        }
                    }
                    else if (machine?.selectedCassette?.selectedFeeder?.part?.TemplateFileName != null)
                    {
                        FindSelectedPartInImage(camera.cameraImage);
                        Feeder feeder = machine.selectedCassette.selectedFeeder;
                        if (camera.IsPartInView)
                            OpenCvSharp.Cv2.DrawMarker(camera.cameraImage, new OpenCvSharp.Point(x_part_cursor, y_part_cursor), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                    }
                    else
                    {
                        UpdateSecondaryVideoStream(camera.cameraImage);
                    }
                                        
                    OpenCvSharp.Cv2.DrawMarker(camera.cameraImage, new OpenCvSharp.Point(camera.cameraImage.Cols/2, camera.cameraImage.Rows/2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (camera.SelectedVisualizationViewItem.viewName.Equals("Normal View"))
                                FrameImage.Source = camera.cameraImage.ToWriteableBitmap();
                            else if (camera.SelectedVisualizationViewItem.viewName.Equals("Grayscale"))
                                FrameImage.Source = camera.grayImage.ToWriteableBitmap();
                            else if (camera.SelectedVisualizationViewItem.viewName.Equals("Threshold"))
                                FrameImage.Source = camera.thresImage.ToWriteableBitmap();
                            else if (camera.SelectedVisualizationViewItem.viewName.Equals("Edge Image"))
                                FrameImage.Source = camera.edgeImage.ToWriteableBitmap();
                            else if (camera.SelectedVisualizationViewItem.viewName.Equals("Dilated Image"))
                                FrameImage.Source = camera?.dilatedImage.ToWriteableBitmap();
                        }catch(Exception ex)
                        {
                            Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                        }
                    });

                }
                camera.cameraImage.Release();
                Cv2.WaitKey(10);
            }
        }

        private void UpdateSecondaryVideoStream(Mat srcImage)
        {
            /* Not sure what to do but this prevents memory leaks */
            
            // Convert the image to grayscale
            Cv2.CvtColor(srcImage, camera.grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(camera.grayImage, camera.grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(camera.grayImage, camera.thresImage, 80, 255, ThresholdTypes.Binary);

            // Use Canny edge detection to find edges in the image
            Cv2.Canny(camera.thresImage, camera.edgeImage, 50, 150, 3);

            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(camera.edgeImage, camera.dilatedImage, null, iterations: 2);

            return;
        }

        private OpenCvSharp.Rect FindPickInImage(Mat srcImage)
        {
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (Constants.CAMERA_FRAME_WIDTH / 4);
            int y = 0;
            int width = (Constants.CAMERA_FRAME_WIDTH / 2);
            int height = (Constants.CAMERA_FRAME_WIDTH / 18);
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, width, height);

            //OpenCvSharp.Rect roi = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            // ROI
            camera.pickROI = new Mat(srcImage, roi); 

            // Convert the image to grayscale
            Cv2.CvtColor(camera.pickROI, camera.grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(camera.grayImage, camera.grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(camera.grayImage, camera.thresImage, 80, 255, ThresholdTypes.Binary);

            // Use Canny edge detection to find edges in the image
            Cv2.Canny(camera.thresImage, camera.edgeImage, 50, 150, 3);

            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(camera.edgeImage, camera.dilatedImage, null, iterations: 2);

            // Find contours in the image
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(camera.dilatedImage, out OpenCvSharp.Point[][] contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);

            double longestLen = 0.0;
            OpenCvSharp.Rect longestRect = new OpenCvSharp.Rect(0, 0, 0, 0);

            foreach (var contour in contours)
            {
                double contourLength = Cv2.ArcLength(contour, true);
                if (contourLength > longestLen)
                {
                    longestLen = contourLength;
                    longestRect = Cv2.BoundingRect(contour);
                }
            }
            longestRect.X += x; longestRect.Y += y;
            return longestRect;
        }

        private OpenCvSharp.Rect FindRectangleInImage(Mat srcImage) {

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
            return largestRect;
        }
               

        private void FindSelectedPartInImage(Mat image)
        {
            Mat grayTemplateImage = new Mat();
            Mat matchResultImage = new Mat();
            double x_next = 0, y_next = 0, y_last = 0;
            double x_offset_pixels, y_offset_pixels;
            double x_next_part, y_next_part;
            bool partInView = false;
            Mat res_32f;

            machine.selectedCassette.selectedFeeder.part.Template = new Mat(machine.selectedCassette.selectedFeeder.part.TemplateFileName);
            
            Mat template = machine.selectedCassette.selectedFeeder.part.Template;

            Cv2.CvtColor(image, camera.grayImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(camera.grayImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(matchResultImage, MatType.CV_8U, 255.0);
            
            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(matchResultImage, camera.thresImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold);
            
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
                    x_offset_pixels = x_next - (0.5 * Constants.CAMERA_FRAME_WIDTH);
                    y_offset_pixels = -(y_next - (0.5 * Constants.CAMERA_FRAME_HEIGHT));
                    x_next_part = machine.CurrentX + (x_offset_pixels * machine.GetImageScaleAtDistanceX(machine.CurrentZ - 15));
                    y_next_part = machine.CurrentY + (y_offset_pixels * machine.GetImageScaleAtDistanceY(machine.CurrentZ - 15));
                    if(machine.selectedCassette.selectedFeeder.SetCandidateNextPartLocation(x_next_part, y_next_part) == true)
                    {
                        partInView = true;
                        x_part_cursor = x_next; y_part_cursor = y_next;
                    }
                    break;
                }
            }
            camera.IsPartInView = partInView;
        }
    }
}
