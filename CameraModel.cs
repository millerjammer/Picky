using OpenCvSharp;
using OpenCvSharp.Internal.Vectors;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Media3D;
using Xamarin.Forms;
using Image = System.Windows.Controls.Image;

namespace Picky
{
    public class CameraModel : INotifyPropertyChanged
    {
        public MachineModel machine { get; set; }
        public  VideoCapture capture { get; set; }
        private Mat RawImage { get; set; }

        /* Mats for general availability */
        public Mat ColorImage { get; set; }
        public Mat GrayImage { get; set; }
        public Mat ThresImage { get; set; }
        public Mat EdgeImage { get; set; }
        public Mat DilatedImage { get; set; }
              
        public Mat CircleROI { get; set; }
        public Mat RectangleROI { get; set; }
        public Mat QRImageROI { get; set; }

        /* What we are currently viewing */
        public VisualizationStyle SelectedVisualizationViewItem { get; set; }

        /* What we are currently viewing */
        public ImageProcessingStyle SelectedImagrProcessingStyle { get; set; }

        /* Mats for Part Identification */
        private Mat grayTemplateImage;
        private Mat matchResultImage;

        private Point2d nextPartOffset;
        public Part PartToFind { get; set; }
        private OpenCvSharp.Rect partROI;
        
        /* Circle Request */
        private bool searchCircleRequest = false;
        private CircleDetector circleDetector;
        private CircleSegment bestCircle;
        private CircleSegment[] Circles;

        /* QR Request */
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

        private bool isManualFocus;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; SetCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }

        private int binaryThreshold;
        public int BinaryThreshold
        {
            get { return binaryThreshold; }
            set { binaryThreshold = value; OnPropertyChanged(nameof(BinaryThreshold)); }
        }

        private int circleDetectorP1;
        public int CircleDetectorP1
        {
            get { return circleDetectorP1; }
            set { circleDetectorP1 = value; OnPropertyChanged(nameof(CircleDetectorP1)); }
        }

        private double circleDetectorP2;
        public double CircleDetectorP2
        {
            get { return circleDetectorP2; }
            set { circleDetectorP2 = value; OnPropertyChanged(nameof(CircleDetectorP2)); }
        }

        private bool isManualToolTipSearch;
        public bool IsManualToolTipSearch
        {
            get { return isManualToolTipSearch; }
            set { isManualToolTipSearch = value; SetManualCircleSearch(); OnPropertyChanged(nameof(IsManualToolTipSearch)); }
        }
     

        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; SetCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

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
            
            partROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), (Constants.CAMERA_FRAME_HEIGHT / 2), Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 2);

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

        public void CancelAllRequests()
        {
            searchCircleRequest = false;
            searchQRRequest = false;
           
        }

        public void SetManualCircleSearch()
        {
            //CircleDetector detector = new CircleDetector(HoughModes.GradientAlt, machine.SelectedPickTool.CircleDetectorP1, machine.SelectedPickTool.CircleDetectorP2, machine.SelectedPickTool.MatUpperThreshold);
            //detector.ROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), 0, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 4);
            //detector.zEstimate = 25.0;
            //detector.CircleEstimate = new CircleSegment(new Point2f(0, 0), (float)(Constants.TOOL_28GA_TIP_DIA_MM / 2));
            //RequestCircleLocation(detector);
        }
           

        public void SetCameraFocus()
        {
            if (IsManualFocus == true)
            {
                int value = (int)capture.Get(VideoCaptureProperties.Focus);
                capture.Set(VideoCaptureProperties.Focus, Focus);
                Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                capture.Set(VideoCaptureProperties.AutoFocus, 1);
                Console.WriteLine("Auto Focus Mode");
            }
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       
        public void RequestPartLocation(OpenCvSharp.Rect roi)
        {
            partROI = roi;
        }

        public CircleSegment GetBestCircle()
        {
            Console.WriteLine("Best Circle (in pixels): " + bestCircle.ToString());
            return bestCircle;
        }

        public bool IsCircleSearchActive()
        {
            return searchCircleRequest;
        }

        public void RequestCircleLocation(CircleDetector detector)
        /*-------------------------------------------------------------------------
         * Call here to start the process, use IsCircleSearchActive to monitor
         * progress and GetBestCircle when it's no longer active. roi is the region 
         * of interest in pixels.  sc is the circle to find, an estimate in mm.  ZOptical in mm
         * ZOptical should include length of the tool.
         * Note: zOpticalDistance is needed ONLY to qualify the circles we are searching for
         * TODO - eliminate zOpticalDistance.
         * ------------------------------------------------------------------------*/
        {
            //sc is in MM
            circleDetector = detector;
            searchCircleRequest = true;
            
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

        /** Public Methods for Part location **/
        public Point2d GetNextPartOffset()
        {
            return nextPartOffset;
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

        private bool FindCircleInROI()
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

            CircleROI = new Mat(DilatedImage, circleDetector.ROI);

            //Convert the estimated circle (in MM) to search criteria (in Pix) based on z.  
            var scale = machine.Cal.GetScaleMMPerPixAtZ(circleDetector.zEstimate);
            int minR = (int)((circleDetector.CircleEstimate.Radius / 4) / scale.xScale);
            int maxR = (int)((circleDetector.CircleEstimate.Radius * 2) / scale.xScale);
            Console.WriteLine("minR/maxR (pix): " + minR + "," + maxR + "@" + scale.xScale);

            double param1 = machine.SelectedPickTool.UpperCircleDetector.Param1;
            double param2 = machine.SelectedPickTool.UpperCircleDetector.Param2;
            if (!IsManualToolTipSearch)
            {
                param1 = circleDetector.Param1;
                param2 = circleDetector.Param2;
            }


            //Set Min distance between matches
            int minDist = maxR;

            // Find circles using HoughCircles
            Circles = Cv2.HoughCircles(
                CircleROI,
                circleDetector.DetectorType,        // Usually HoughModes.Gradient or HoughModes.GradientAlt
                dp: 1.5,
                minDist: minDist,                   //Was 800, formally set to 2xmin raduis
                param1: param1,      //smaller means more false circles
                param2: param2,      //smaller means more false circles
                minRadius: minR,            
                maxRadius: maxR);
            
            if (Circles.Length == 0)
            {
                Console.WriteLine("No circles found.");
                return false;
            }
            else
            {
                // Calculate the center of the image, in pixels
                Point2f imageCenter = new Point2f(CircleROI.Width / 2.0f, CircleROI.Height / 2.0f);
                // Find the closest circle to the center
                CircleSegment closestCircle = Circles.OrderBy(circle => Math.Pow(circle.Center.X - imageCenter.X, 2) + Math.Pow(circle.Center.Y - imageCenter.Y, 2)).First();
                // Calculate the offset of the closest circle from the center of the image, in pixels
                Point2f CircleToFind = new Point2f(closestCircle.Center.X - imageCenter.X, closestCircle.Center.Y - imageCenter.Y);
                bestCircle = closestCircle;
                bestCircle.Center = CircleToFind;
            }
            OpenCvSharp.Cv2.Rectangle(ColorImage, circleDetector.ROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
            return true;
        }

        private bool FindPartInImage(Part part, Mat grayImage)
        {
            /*------------------------------------------------------
             * Private function for finding parts.  This is the default
             * function and runs when a Part is selected
             * 
             * -----------------------------------------------------*/

            double x_next = 0, y_next = 0, y_last = 0;
            int match_count = 0;
            double x_next_part, y_next_part;
            Mat res_32f;
            Mat image, thresImage;
            Mat template = part.Template;

            try
            {
                //Convert template to gray
                Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                image = new Mat(grayImage, partROI);
                res_32f = new Mat(image.Rows - template.Rows + 1, image.Cols - template.Cols + 1, MatType.CV_32FC1);
                Cv2.MatchTemplate(image, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);
            }catch (Exception e)
            {
                Console.WriteLine($"Error matching: {e.Message}");
                return false;
            };

            /* Show Result */
            res_32f.ConvertTo(matchResultImage, MatType.CV_8U, 255.0);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            thresImage = new Mat(ThresImage, partROI);
            Cv2.AdaptiveThreshold(matchResultImage, thresImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, part.PartDetectionThreshold);

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
                    //Location within the ROI
                    x_next = (maxloc.X + template.Cols / 2);
                    y_next = (maxloc.Y + template.Rows / 2);
                }

                if (maxval > 0)
                {
                    OpenCvSharp.Rect rect = new OpenCvSharp.Rect(maxloc.X + partROI.X, maxloc.Y + partROI.Y, template.Cols, template.Rows);
                    Cv2.Rectangle(ColorImage, rect, new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(thresImage, maxloc, 0); //mark drawn blob so we don't find it again
                    match_count++;
                }
                else if (match_count > 0)
                {
                    part.IsInView = true;
                    // Tell Part where to pick the next part
                    x_next += partROI.X;
                    y_next += partROI.Y;
                    machine.selectedCassette.selectedFeeder.SetCandidateNextPartLocation(x_next, y_next, Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE + machine.Cal.ZCalPadZ);
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
                    Cv2.Threshold(GrayImage, ThresImage, BinaryThreshold, 255, ThresholdTypes.Binary);  // Default is 80
                    Cv2.Canny(ThresImage, EdgeImage, 50, 150, 3);  //50 150 3
                    Cv2.Dilate(EdgeImage, DilatedImage, null, iterations: 2);
                    
                    RawImage = null;  // hint garbage collection
                }
                catch (Exception ex)
                {
                   GC.Collect();
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
                        OpenCvSharp.Cv2.Rectangle(ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
                        searchQRRequest = false;        //Don't cancel till you get a QR
                    }
                    else
                    {
                        OpenCvSharp.Cv2.Rectangle(ColorImage, searchQRROI, new OpenCvSharp.Scalar(0, 0, 255), 4);
                    }
                }
                else if (searchCircleRequest)
                {
                    if (FindCircleInROI())
                    {
                        foreach (CircleSegment circle in Circles)
                        {
                            // Draw the circle outline
                            int x = (int)(circle.Center.X + circleDetector.ROI.X);
                            int y = (int)(circle.Center.Y + circleDetector.ROI.Y);
                            Cv2.Circle(ColorImage, x, y, (int)circle.Radius, Scalar.Green, 2);
                            // Draw the circle center
                            Cv2.Circle(ColorImage, x, y, 3, Scalar.Red, 3);
                            if (!IsManualToolTipSearch)
                            {
                                searchCircleRequest = false;        //Don't cancel request till you get a circle
                            }
                        }
                    }
                }
                else if (PartToFind != null)
                {
                    FindPartInImage(PartToFind, GrayImage);
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
                        //Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                    }
                });
                Cv2.WaitKey(1);

            }
        }
    }
}
