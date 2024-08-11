using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Picky
{
    public class CameraModel : INotifyPropertyChanged
    {
        private MachineModel machine;
        private Mat RawImage { get; set; }

        /* Mats for general availability */
        public Mat ColorImage { get; set; }
        public Mat GrayImage { get; set; }
        public Mat ThresImage { get; set; }
        public Mat EdgeImage { get; set; }
        public Mat DilatedImage { get; set; }

        public Mat PickROI { get; set; }
        public Mat CircleROI { get; set; }
        public Mat QRImageROI { get; set; }

        /* What we are currently viewing */
        public VisualizationStyle SelectedVisualizationViewItem { get; set; }

        public bool SuspendImageProcessing = false;

        /* Mats for Part Identification */
        private Mat grayTemplateImage;
        private Mat matchResultImage;

        public VideoCapture capture { get; set; }

        private bool searchCircleRequest = false;
        private OpenCvSharp.Rect searchCircleROI;
        private CircleSegment searchCircleToFind;
        private CircleSegment bestCircle;

        public bool searchQRRequest = false;
        private OpenCvSharp.Rect searchQRROI;
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
        public Image FrameImage
        {
            get { return frameImage; }
            set { frameImage = value; OnPropertyChanged(nameof(FrameImage)); } //Notify listeners
        }

        public Part PartToFind { get; set; }

        public int Focus { get; set; }
        public bool IsManualFocus { get; set; }
        public Mat selectedViewMat { get; set; }


        private readonly BackgroundWorker bkgWorker;

        public CameraModel(int cameraIndex, MachineModel mm)
        {
            machine = mm;

            RawImage = new Mat();
            ColorImage = new Mat();
            GrayImage = new Mat();
            ThresImage = new Mat();
            EdgeImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            DilatedImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            PickROI = new Mat();

            grayTemplateImage = new Mat();
            matchResultImage = new Mat();
                        
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


        /** Public methods for Circles **/

        public CircleSegment GetBestCircle()
        {
            Console.WriteLine("Best Circle (in pixels): " + bestCircle.ToString());
            return bestCircle;
        }

        public bool IsCircleSearchActive()
        {
            return searchCircleRequest;
        }

        public void RequestCircleLocation(OpenCvSharp.Rect roi, CircleSegment sc)
        /*-------------------------------------------------------------------------
         * Call here to start the process, use IsCircleSearchActive to monitor
         * progress and GetBestCircle when it's no longer active. roi is the region 
         * of interest in pixels.  sc is the circle to find, an estimate in mm
         * ------------------------------------------------------------------------*/
        {
            //sc is in MM
            searchCircleRequest = true;
            searchCircleToFind = sc;
            searchCircleROI = roi;
        }

        /** Public methods for QR Code **/

        public string[] GetQRCode()
        {
            return currentQRCode;
        }

        public bool IsQRSearchActive()
        {
            return searchQRRequest;
        }

        public void RequestQRCodeLocation(OpenCvSharp.Rect roi)
        /*-------------------------------------------------------------------------
        * Call here to start the process, use IsQRSearchActive to monitor
        * progress and GetQRCode when it's no longer active
        * ------------------------------------------------------------------------*/
        {
            searchQRRequest = true;
            searchQRROI = roi;
        }

        private bool DecodeQRCode()
        {
            /************************************************************************
             * Private function for decoding QR codes
             * 
             * Use: 
             * RequestQRCode()
             * GetQRCode()
             * IsQRSearchActive()
             * 
             * ***********************************************************************/

            try
            {
                using (var detector = new QRCodeDetector())
                {
                    QRImageROI = new Mat(GrayImage, searchQRROI);

                    detector.DetectMulti(QRImageROI, out currentQRCodePoints);
                    if (currentQRCodePoints.Length <= 0)
                        return false;
                    detector.DecodeMulti(QRImageROI, currentQRCodePoints, out currentQRCode);
                    for (int i = 0; i < currentQRCode.Length; i++)
                    {
                        // Offset all point to reference a full frame since we might have specified a roi
                        for (int j = 0; j < 4; j++)
                        {
                            currentQRCodePoints[(4 * i) + j].X += searchQRROI.X;
                            currentQRCodePoints[(4 * i) + j].Y += searchQRROI.Y;
                        }
                        Console.WriteLine("QR: " + currentQRCode[i] + " " + currentQRCodePoints[ (4 * i) ] + " " + currentQRCodePoints[ (4 * i) + 1] + " " + currentQRCodePoints[(4 * i) + 2] + " " + currentQRCodePoints[(4 * i) + 3]);
                    }
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

        private bool FindCircleInROI(bool showResult)
        {
            /****************************************************************************
             * Private function for finding circles.
             * Returns false if no circle found, sets bestCircle to 0,0 r0
             * Returns true if circle found, call GetBestCircle() to get CircleSegement
             * where center contains the offset from the center of the image.
             * ROI in pixels.
             * Use:
             * IsCircleSearchActive() to see if a search is active 
             * GetBestCircle() to get result of last successful search. 
             * RequestCircleLocation(OpenCvSharp.Rect roi) to start search of ROI
             * 
             *****************************************************************************/

            //Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();

            CircleROI = new Mat(DilatedImage, searchCircleROI);

            //Convert the estimated circle (in MM) to search criteria (in Pix) based on z.  
            var scale = machine.Cal.GetScaleMMPerPixAtZ(machine.CurrentZ + Constants.TOOL_LENGTH_MM);
            int minR = (int)((searchCircleToFind.Radius * .25) / scale.xScale);
            int maxR = (int)((searchCircleToFind.Radius * 2) / scale.yScale);

            //Set Min distance between matches
            int minDist = (int)(searchCircleROI.Width/6);

            // Find circles using HoughCircles
            CircleSegment[] circles = Cv2.HoughCircles(
                CircleROI,
                HoughModes.GradientAlt,
                dp: 1.5,
                minDist: minDist,   //Was 800, formally set to 2xmin raduis
                param1: 50,     //smaller means more false circles
                param2: .8,    //smaller means more false circles
                minRadius: minR, // Was 600,
                maxRadius: maxR);  // Was 1200

            if (circles.Length == 0)
            {
                Console.WriteLine("No circle found.");
                bestCircle = new CircleSegment();
                return false;
            }

            // Calculate the center of the image, in pixels
            Point2f imageCenter = new Point2f(CircleROI.Width / 2.0f, CircleROI.Height / 2.0f);
            // Find the closest circle to the center
            CircleSegment closestCircle = circles.OrderBy(circle => Math.Pow(circle.Center.X - imageCenter.X, 2) + Math.Pow(circle.Center.Y - imageCenter.Y, 2)).First();
            // Calculate the offset of the closest circle from the center of the image, in pixels
            Point2f CircleToFind = new Point2f(closestCircle.Center.X - imageCenter.X, closestCircle.Center.Y - imageCenter.Y);
            bestCircle = closestCircle;
            bestCircle.Center = CircleToFind;
           
            if (showResult)
                showCircleResult(CircleROI, circles);

            return true;

        }

        private void showCircleResult(Mat dest_result_mat, CircleSegment[] circles)
        {
            Mat ColorCircleROI = new Mat();
            Cv2.CvtColor(dest_result_mat, ColorCircleROI, ColorConversionCodes.GRAY2BGR);
            foreach (CircleSegment circle in circles)
            {
                // Draw the circle outline
                Cv2.Circle(ColorCircleROI, (int)circle.Center.X, (int)circle.Center.Y, (int)circle.Radius, Scalar.Green, 2);
                // Draw the circle center
                Cv2.Circle(ColorCircleROI, (int)circle.Center.X, (int)circle.Center.Y, 3, Scalar.Red, 3);
            }
            Cv2.ImShow("Detected Circles", ColorCircleROI);
            Cv2.WaitKey(1);
        }

        private bool FindPartInImage(Part part, Mat image)
        {

            double x_next = 0, y_next = 0, y_last = 0;
            int match_count = 0;
            double x_offset_pixels, y_offset_pixels;
            double x_next_part, y_next_part;
            Mat res_32f;

            Mat template = part.Template;

            try
            {
                Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
                Cv2.MatchTemplate(GrayImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);
            }catch (Exception e)
            {
                Console.WriteLine($"Error matching: {e.Message}");
                return false;
            };

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
                else if (match_count > 0)
                {
                    part.IsInView = true;
                    // Tell Part where to pick the next part
                    x_offset_pixels = (x_next - (0.5 * Constants.CAMERA_FRAME_WIDTH));
                    y_offset_pixels = (y_next - (0.5 * Constants.CAMERA_FRAME_HEIGHT));
                    var scale = machine.Cal.GetScaleMMPerPixAtZ(machine.CurrentZ);
                    x_next_part = machine.CurrentX - (x_offset_pixels * scale.xScale);
                    y_next_part = machine.CurrentY + (y_offset_pixels * scale.yScale);
                    machine.selectedCassette.selectedFeeder.SetCandidateNextPartLocation(x_next_part, y_next_part);
                    //Console.WriteLine("Count: " + match_count + " " + x_next_part + " " + y_next_part);
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
                
        public void Dispose()
        {
            bkgWorker.CancelAsync();
        }



        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
        /*----------------------------------------------------------------
         * This is the real-time worker - don't abuse it
         * 
         * --------------------------------------------------------------*/

            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                try
                {
                    RawImage = capture.RetrieveMat();
                    Cv2.CopyTo(RawImage, ColorImage);
                    Cv2.CvtColor(RawImage, GrayImage, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(GrayImage, GrayImage, new OpenCvSharp.Size(5, 5), 0);
                    Cv2.Threshold(GrayImage, ThresImage, 80, 255, ThresholdTypes.Binary);
                    Cv2.Canny(ThresImage, EdgeImage, 50, 150, 3);
                    Cv2.Dilate(EdgeImage, DilatedImage, null, iterations: 2);
                    RawImage = null;  // hint garbage collection
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ouchy - " + ex.ToString());
                    GC.Collect();
                    // Wait for all finalizers to complete before continuing.
                    GC.WaitForPendingFinalizers();
                }

                if (searchQRRequest)
                {
                    if (DecodeQRCode())
                    {
                        for (int i = 0; i < currentQRCodePoints.Length; i++)
                        {
                            OpenCvSharp.Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(currentQRCodePoints[i].X, currentQRCodePoints[i].Y), new OpenCvSharp.Scalar(255, 0, 0), OpenCvSharp.MarkerTypes.Cross, 200, 4);
                        }
                        OpenCvSharp.Cv2.Rectangle (ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
                        searchQRRequest = false;
                    }
                    else
                    {
                        OpenCvSharp.Cv2.Rectangle(ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 0, 255), 4);
                    }
                }
                else if (PartToFind != null)
                {
                      FindPartInImage(PartToFind, ColorImage);
                }
                else if (searchCircleRequest)
                {
                    if(FindCircleInROI(true))
                        OpenCvSharp.Cv2.Rectangle(ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
                    else
                        OpenCvSharp.Cv2.Rectangle(ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 0, 255), 4);
                    searchCircleRequest = false;
                }
                                    

                /* Draw default cursor */
                OpenCvSharp.Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(ColorImage.Cols / 2, ColorImage.Rows / 2), new OpenCvSharp.Scalar(0, 0, 255), OpenCvSharp.MarkerTypes.Cross, 100, 1);
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
                Cv2.WaitKey(1);

            }
        }
    }
}
