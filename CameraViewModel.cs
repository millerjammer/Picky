using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Picky
{
    public class VisualizationStyle
    {
        public string viewName { get; set; }
        public Mat viewMat { get; set; }
        public VisualizationStyle(string name, Mat mati)
        {
            viewName = name;
            viewMat = mati;
        }
    }

    public class CameraSelection
    {
        public string cameraName { get; set; }
        public VideoCapture cameraCapture { get; set; }
        public CameraSelection(string name, VideoCapture capture)
        {
            cameraName = name;
            cameraCapture = capture;
        }
    }

    public class CameraViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        private readonly BackgroundWorker bkgWorker;

        public List<CameraSelection> CameraIndexView { get; set; }
        public CameraSelection SelectedCameraIndexViewItem { get; set; }
        
        public VideoCapture captureDown = new VideoCapture();
        public VideoCapture captureUp = new VideoCapture();

        public List<VisualizationStyle> VisualizationView { get; set; }
        public VisualizationStyle SelectedVisualizationViewItem {  get; set; }

        public Mat cameraImage = new Mat();
        public Mat grayImage = new Mat();
        public Mat thresImage = new Mat();
        public Mat edgeImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
        public Mat dilatedImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
        public Mat pickROI = new Mat();

        public Image frameImage;

        private double x_part_cursor, y_part_cursor;

        public SolidColorBrush PartInViewIconColor
        {
            get { if(IsPartInView) return (new SolidColorBrush(Color.FromArgb(128, 0, 255, 0))); else return (new SolidColorBrush(Color.FromArgb(128, 255, 0, 0))); }
        }

        private bool isPartInView = false;
        public bool IsPartInView 
        {
            get { return isPartInView; }
            set { if(isPartInView == value) return; isPartInView = value; OnPropertyChanged(nameof(PartInViewIconColor)); }
        }

        private bool isManualFocus;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; setCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }
        

        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; setCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

        private bool isCassetteFeederSelected = false;
        public bool IsCassetteFeederSelected 
        {  
            get { return isCassetteFeederSelected;  }
        }

        private int detectionThreshold;
        public int DetectionThreshold
        {
            get { return detectionThreshold;  }
            set { detectionThreshold = value; machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold = (double)detectionThreshold;  OnPropertyChanged(nameof(DetectionThreshold)); }
        }

        public CameraViewModel(Image iFrame)
        {
            //this.capture = cap;

            frameImage = iFrame;

            captureDown.Open(2);
            captureDown.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            captureDown.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            captureDown.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));


            captureDown.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            captureDown.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            captureDown.Set(VideoCaptureProperties.AutoExposure, 1);
            captureDown.Set(VideoCaptureProperties.Brightness, .2);
            captureDown.Set(VideoCaptureProperties.BufferSize, 200);


            captureUp.Open(0);
            captureUp.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            captureUp.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            captureUp.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));


            captureUp.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            captureUp.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            captureUp.Set(VideoCaptureProperties.AutoExposure, 1);
            captureUp.Set(VideoCaptureProperties.Brightness, .2);
            captureUp.Set(VideoCaptureProperties.BufferSize, 200);

            /* Listen for selectedCassette to change, then we can listen for selectedFeeder */
            machine.PropertyChanged += OnMachinePropertyChanged;

            VisualizationView = new List<VisualizationStyle>
            {
                new VisualizationStyle("Normal", cameraImage),
                new VisualizationStyle("Grayscale", grayImage),
                new VisualizationStyle("Threshold", thresImage),
                new VisualizationStyle("Edge Image", edgeImage),
                new VisualizationStyle("Dilated Image", dilatedImage),
            };
            SelectedVisualizationViewItem = VisualizationView.FirstOrDefault();

            CameraIndexView = new List<CameraSelection>
            {
                new CameraSelection("Up Camera", captureUp),
                new CameraSelection("Down Camera", captureDown),
            };
            SelectedCameraIndexViewItem = CameraIndexView.FirstOrDefault();

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;
            bkgWorker.RunWorkerAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
               
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on the machine, notify the view to update itself
            if (machine.selectedCassette == null || machine.selectedCassette.selectedFeeder == null)
            {
                isCassetteFeederSelected = false;
            }
            else
            {
                /* Machine changed, listen to cassett property changes */
                machine.selectedCassette.PropertyChanged += OnMachinePropertyChanged;
                /* TODO, get rid of this, do in setter/getter */
                detectionThreshold = (int)machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold;
                isCassetteFeederSelected = true;
            }
            OnPropertyChanged("IsCassetteFeederSelected");
            OnPropertyChanged("DetectionThreshold");
        }

        public void Dispose()
        {
            bkgWorker.CancelAsync();
            captureUp.Dispose();
            captureDown.Dispose();

        }

        public void setCameraFocus()
        {
            captureUp.AutoFocus = !IsManualFocus;
            if (IsManualFocus)
            {
               int value = (int)SelectedCameraIndexViewItem.cameraCapture.Get(VideoCaptureProperties.Focus);
                SelectedCameraIndexViewItem.cameraCapture.Set(VideoCaptureProperties.Focus, Focus);
               Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                Console.WriteLine("Auto Focus Mode");
            }
        }

        public ICommand FullScreenCommand { get { return new RelayCommand(FullScreen); } }
        private void FullScreen()
        {
            Console.WriteLine("Full Screen Clicked");
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                try
                {
                    /* TODO RetrieveMat directly won't display cameraImage so we have to do a cvtColor */
                    Cv2.CvtColor(SelectedCameraIndexViewItem.cameraCapture.RetrieveMat(), cameraImage, ColorConversionCodes.BGR2BGRA);
                    machine.currentRawImage = cameraImage.Clone();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ouchy - " + ex.ToString());
                }
                if (machine.CameraCalibrationState == MachineModel.CalibrationState.InProcess)
                {
                    machine.CalRectangle = FindRectangleInImage(cameraImage);
                    Cv2.Rectangle(cameraImage, machine.CalRectangle, new OpenCvSharp.Scalar(255, 0, 255), 2);
                }
                else if (machine.PickCalibrationState == MachineModel.CalibrationState.InProcess)
                {
                    IsManualFocus = true;
                    Focus = Constants.CALIBRATION_PICK_FOCUS;
                    machine.CalRectangle = FindPickInImage(cameraImage);
                    Cv2.Rectangle(cameraImage, machine.CalRectangle, new OpenCvSharp.Scalar(0, 255, 0), 2);
                    double x_center = (machine.CalRectangle.X + (machine.CalRectangle.Width / 2));
                    double x_offset = x_center - (Constants.CAMERA_FRAME_WIDTH / 2);
                    double y_end = machine.CalRectangle.Height;
                    double y_offset = (Constants.CAMERA_FRAME_HEIGHT / 2) - y_end;
                    /* x_offset and y_offset are measured from center of the frame, in pixels. x and y are generally positive. Actual location in quadrant 1  */
                    OpenCvSharp.Cv2.DrawMarker(cameraImage, new OpenCvSharp.Point(x_center, y_end), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
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
                    FindSelectedPartInImage(cameraImage);
                    Feeder feeder = machine.selectedCassette.selectedFeeder;
                    if (IsPartInView)
                        OpenCvSharp.Cv2.DrawMarker(cameraImage, new OpenCvSharp.Point(x_part_cursor, y_part_cursor), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                }
                else
                {
                    UpdateSecondaryVideoStream();
                }

                OpenCvSharp.Cv2.DrawMarker(cameraImage, new OpenCvSharp.Point(cameraImage.Cols / 2, cameraImage.Rows / 2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        frameImage.Source = SelectedVisualizationViewItem.viewMat.ToWriteableBitmap();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                    }
                });
                Cv2.WaitKey(10);
            }
        }

        private void UpdateSecondaryVideoStream()
        {
            /* Not sure what to do but this prevents memory leaks */
            if(cameraImage. Empty()) return;

            // Convert the image to grayscale
            Cv2.CvtColor(cameraImage, grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(grayImage, thresImage, 80, 255, ThresholdTypes.Binary);

            // Use Canny edge detection to find edges in the image
            Cv2.Canny(thresImage, edgeImage, 50, 150, 3);

            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(edgeImage, dilatedImage, null, iterations: 2);

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
            pickROI = new Mat(srcImage, roi);

            // Convert the image to grayscale
            Cv2.CvtColor(pickROI, grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(grayImage, thresImage, 80, 255, ThresholdTypes.Binary);

            // Use Canny edge detection to find edges in the image
            Cv2.Canny(thresImage, edgeImage, 50, 150, 3);

            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(edgeImage, dilatedImage, null, iterations: 2);

            // Find contours in the image
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(dilatedImage, out OpenCvSharp.Point[][] contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);

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

        private OpenCvSharp.Rect FindRectangleInImage(Mat srcImage)
        {

            // Convert the image to grayscale
            Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGR2GRAY);

            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(grayImage, grayImage, new OpenCvSharp.Size(5, 5), 0);

            // Binary threshold
            Cv2.Threshold(grayImage, thresImage, 100, 255, ThresholdTypes.Binary);

            // Use Canny edge detection to find edges in the image
            Cv2.Canny(thresImage, edgeImage, 50, 150, 3);

            // Dilate the edges to fill gaps in between object edges
            Cv2.Dilate(edgeImage, dilatedImage, null, iterations: 2);

            // Find contours in the image
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(dilatedImage, out OpenCvSharp.Point[][] contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);

            double largestArea = 0;
            OpenCvSharp.Rect largestRect = new OpenCvSharp.Rect(0, 0, 0, 0);

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

            Cv2.CvtColor(image, grayImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(grayImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(matchResultImage, MatType.CV_8U, 255.0);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(matchResultImage, thresImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold);

            y_last = Constants.CAMERA_FRAME_HEIGHT;
            /* Show all matches */
            while (true)
            {
                double minval, maxval;
                OpenCvSharp.Point minloc, maxloc;
                Cv2.MinMaxLoc(thresImage, out minval, out maxval, out minloc, out maxloc);
                if (maxloc.Y > 0 && maxloc.X > 0 && maxloc.Y < y_last)
                {
                    y_last = maxloc.Y;
                    x_next = (maxloc.X + template.Cols / 2);
                    y_next = (maxloc.Y + template.Rows / 2);
                }

                if (maxval > 0)
                {
                    Cv2.Rectangle(image, maxloc, new OpenCvSharp.Point(maxloc.X + template.Cols, maxloc.Y + template.Rows), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(thresImage, maxloc, 0); //mark drawn blob
                }
                else
                {
                    x_offset_pixels = x_next - (0.5 * Constants.CAMERA_FRAME_WIDTH);
                    y_offset_pixels = -(y_next - (0.5 * Constants.CAMERA_FRAME_HEIGHT));
                    x_next_part = machine.CurrentX + (x_offset_pixels * machine.GetImageScaleAtDistanceX(machine.CurrentZ + Constants.FEEDER_TO_PLATFORM_ZOFFSET));
                    y_next_part = machine.CurrentY + (y_offset_pixels * machine.GetImageScaleAtDistanceY(machine.CurrentZ + Constants.FEEDER_TO_PLATFORM_ZOFFSET));
                    if (machine.selectedCassette.selectedFeeder.SetCandidateNextPartLocation(x_next_part, y_next_part) == true)
                    {
                        partInView = true;
                        x_part_cursor = x_next; y_part_cursor = y_next;
                    }
                    break;
                }
            }
            IsPartInView = partInView;
        }

    }
}
