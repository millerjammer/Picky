using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Picky
{
    public class CameraModel : INotifyPropertyChanged
    {
        private Mat RawImage { get; set; }
        
        
        /* Mats for general availability */
        public Mat ColorImage { get; set; }
        public Mat GrayImage { get; set; }
        public Mat ThresImage { get; set; }
        public Mat EdgeImage { get; set; }
        public Mat DilatedImage { get; set; }
        public Mat PickROI { get; set; }

        /* What we are currently viewing */
        public VisualizationStyle SelectedVisualizationViewItem { get; set; }

        /* Mats for Part Identification */
        private Mat grayTemplateImage; 
        private Mat matchResultImage;
        private bool searchQRRequest = false;
        
        public VideoCapture capture { get; set; }

        /* Properties that need to send notifications */
        private string[] currentQRCode;
        public string[] CurrentQRCode
        {
            get { return currentQRCode; }
            set { currentQRCode = value; OnPropertyChanged(nameof(CurrentQRCode)); }
        }
        
        private Point2f[] currentQRCodePoints;
        public Point2f[] CurrentQRCodePoints
        {
            get { return currentQRCodePoints; }
            set { currentQRCodePoints = value; OnPropertyChanged(nameof(CurrentQRCodePoints)); }
        }

        private Image frameImage;
        public Image FrameImage { 
            get { return frameImage; }
            set { frameImage = value; OnPropertyChanged(nameof(FrameImage)); } //Notify listeners
        }

        public Part PartToFind { get; set; }
        public int Focus { get; set; }
        public bool IsManualFocus { get; set; }
        public Mat selectedViewMat { get; set; }

        private readonly BackgroundWorker bkgWorker;

        public CameraModel(int cameraIndex)
        {
            
            RawImage = new Mat();
            ColorImage = new Mat();
            GrayImage = new Mat();
            ThresImage = new Mat();
            EdgeImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            DilatedImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            PickROI = new Mat();

            grayTemplateImage = new Mat();
            matchResultImage = new Mat();

            System.Timers.Timer imgTimer = new System.Timers.Timer();
            imgTimer.Elapsed += new ElapsedEventHandler(OnTimer);
            imgTimer.Interval = 2000;
            imgTimer.Enabled = true;

            capture = new VideoCapture();

            capture.Open(cameraIndex);
            capture.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));


            capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            capture.Set(VideoCaptureProperties.AutoExposure, 1);
            capture.Set(VideoCaptureProperties.Brightness, .2);
            capture.Set(VideoCaptureProperties.BufferSize, 200);

            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;
            bkgWorker.RunWorkerAsync();
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private OpenCvSharp.Rect FindPickInImage(Mat srcImage)
        {
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (Constants.CAMERA_FRAME_WIDTH / 4);
            int y = 0;
            int width = (Constants.CAMERA_FRAME_WIDTH / 2);
            int height = (Constants.CAMERA_FRAME_WIDTH / 18);
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, width, height);
            PickROI = new Mat(srcImage, roi);

            // Find contours in the image
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(DilatedImage, out OpenCvSharp.Point[][] contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);

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

        private bool DecodeQRCode()
        {
            try
            {
                using (var detector = new QRCodeDetector())
                {
                    detector.DetectMulti(GrayImage, out currentQRCodePoints);
                    if (currentQRCodePoints.Length <= 0)
                        return false;
                    detector.DecodeMulti(GrayImage, currentQRCodePoints, out currentQRCode);
                    for (int i = 0; i < currentQRCode.Length; i++)
                        Console.WriteLine("QR: " + currentQRCode[i]);
                    if (currentQRCode.Length > 0)
                        return true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding QR code: {ex.Message}");
                return false;
            }
        }

        private bool FindPartInImage(Part part, Mat image)
        {
            
            double x_next = 0, y_next = 0, y_last = 0;
            int match_count = 0;
           // double x_offset_pixels, y_offset_pixels;
           // double x_next_part, y_next_part;
            Mat res_32f;

            Mat template = part.Template;
            
            Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(GrayImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(matchResultImage, MatType.CV_8U, 255.0);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(matchResultImage, ThresImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, part.PartDetectionThreshold);

            y_last = Constants.CAMERA_FRAME_HEIGHT;
            /* Show all matches */
            while (true)
            {
                double minval, maxval;
                OpenCvSharp.Point minloc, maxloc;
                Cv2.MinMaxLoc(ThresImage, out minval, out maxval, out minloc, out maxloc);
                if (maxloc.Y > 0 && maxloc.X > 0 && maxloc.Y < y_last)
                {
                    y_last = maxloc.Y;
                    x_next = (maxloc.X + template.Cols / 2);
                    y_next = (maxloc.Y + template.Rows / 2);
                }

                if (maxval > 0)
                {
                    Cv2.Rectangle(ColorImage, maxloc, new OpenCvSharp.Point(maxloc.X + template.Cols, maxloc.Y + template.Rows), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(ThresImage, maxloc, 0); //mark drawn blob so we don't find it again
                    match_count++;
                }
                else if(match_count > 0)
                {
                    part.IsInView = true;
                    // Tell Part where to pick the next part
                    //x_offset_pixels = x_next - (0.5 * Constants.CAMERA_FRAME_WIDTH);
                    //y_offset_pixels = -(y_next - (0.5 * Constants.CAMERA_FRAME_HEIGHT));
                    //x_next_part = machine.CurrentX + (x_offset_pixels * machine.GetImageScaleAtDistanceX(machine.CurrentZ + Constants.FEEDER_TO_PLATFORM_ZOFFSET));
                    //y_next_part = machine.CurrentY + (y_offset_pixels * machine.GetImageScaleAtDistanceY(machine.CurrentZ + Constants.FEEDER_TO_PLATFORM_ZOFFSET));
                    //if (machine.selectedCassette.selectedFeeder.SetCandidateNextPartLocation(x_next_part, y_next_part) == true)
                    // {
                    //     IsPartInView = true;
                    //     x_part_cursor = x_next; y_part_cursor = y_next;
                    // }
                    Console.WriteLine("Count: " + match_count);
                    break;
                }
                else
                {
                    part.IsInView = false;
                    break;
                }
            }
            return part.IsInView;
        }
        
        public void OnTimer(object source, ElapsedEventArgs e)
        {
            searchQRRequest = true; 
        }

        public void Dispose()
        {
            bkgWorker.CancelAsync();
        }

        

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (RawImage = capture.RetrieveMat())
                {
                    try
                    {
                        Cv2.CopyTo(RawImage, ColorImage);
                        Cv2.CvtColor(RawImage, GrayImage, ColorConversionCodes.BGR2GRAY);
                        Cv2.GaussianBlur(GrayImage, GrayImage, new OpenCvSharp.Size(5, 5), 0);
                        Cv2.Threshold(GrayImage, ThresImage, 80, 255, ThresholdTypes.Binary);
                        Cv2.Canny(ThresImage, EdgeImage, 50, 150, 3);
                        Cv2.Dilate(EdgeImage, DilatedImage, null, iterations: 2);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ouchy - " + ex.ToString());
                    }

                    if (PartToFind != null)
                    {
                        if (FindPartInImage(PartToFind, ColorImage))
                        {
                            //OpenCvSharp.Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(PartToFind.CenterX, PartToFind.CenterY), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                        }
                    }

                    if (searchQRRequest)
                    {
                        if (DecodeQRCode())
                        {
                            for (int i = 0; i < currentQRCodePoints.Length; i++)
                            {
                                OpenCvSharp.Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(currentQRCodePoints[i].X, currentQRCodePoints[i].Y), new OpenCvSharp.Scalar(255, 0, 0), OpenCvSharp.MarkerTypes.Cross, 200, 4);
                            }
                        }
                        searchQRRequest = false;
                    }

                    OpenCvSharp.Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(RawImage.Cols / 2, RawImage.Rows / 2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (frameImage != null)
                            {
                                frameImage.Source = selectedViewMat.ToWriteableBitmap();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                        }
                    });
                    Cv2.WaitKey(10);
                }
            }
        }
    }
}
