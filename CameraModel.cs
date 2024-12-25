using EnvDTE90;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI;
using System.Windows;
using System.Xml.Linq;
using Xamarin.Forms;
using Application = System.Windows.Application;
using Image = System.Windows.Controls.Image;
using EnvDTE;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.OLE.Interop;

namespace Picky
{
    public class CameraModel : INotifyPropertyChanged
    {
        public MachineModel machine { get; set; }
        
        private Mat RawImage { get; set; }

        /* Mats for general availability */
        public Mat ColorImage { get; set; }
        public Mat GrayImage { get; set; }
        public Mat ThresImage { get; set; }
        public Mat EdgeImage { get; set; }
        public Mat DilatedImage { get; set; }
        public Mat MatchImage { get; set; }

        public Mat CircleROI { get; set; }
        public Mat RectangleROI { get; set; }
        public Mat QRImageROI { get; set; }
        public Mat TemplateROI { get; set; }

        /* What we are currently viewing */
        public VisualizationStyle SelectedVisualizationViewItem { get; set; }

        /* What we are currently viewing */
        public ImageProcessingStyle SelectedImagrProcessingStyle { get; set; }

        /* Mats for Part/Template Identification */
        private Mat grayTemplateImage;
        private Mat matchResultImage;

        private Point2d nextPartOffset;
        public Part PartToFind { get; set; }
        private OpenCvSharp.Rect partROI;

        /* Template Request */
        private bool searchTemplateRequest = false;
        private Mat searchTemplate;
        private OpenCvSharp.Rect searchTemplateROI;
        private List<Position3D> searchTemplateResults = new List<Position3D>();
        public bool IsTemplatePreviewActive { get; set; } = false;

        /* Circle Request */
        private bool searchCircleRequest = false;
        private CircleDetector circleDetector;
        private CircleSegment bestCircle;
        private List<Position3D> bestCircles = new List<Position3D>();
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

        private CameraSettings settings;
        public CameraSettings Settings
        {
            get { return settings; }
            set { settings = value; Console.WriteLine("Camera SettingsUpper Changed"); Settings.ApplySettings(this); OnPropertyChanged(nameof(Settings)); } //Notify listeners
        }

        public Mat selectedViewMat { get; set; }
        public VideoCapture Capture { get; set; }

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
            MatchImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);

            partROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), (Constants.CAMERA_FRAME_HEIGHT / 2), Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 2);
            settings = new CameraSettings();

            Capture = new VideoCapture();

            Capture.Open(cameraIndex);
            Capture.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            Capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            Capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));

            Capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            Capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            Capture.Set(VideoCaptureProperties.AutoExposure, 1);
            Capture.Set(VideoCaptureProperties.Brightness, .2);
            Capture.Set(VideoCaptureProperties.BufferSize, 200);

            grayTemplateImage = new Mat();
            matchResultImage = new Mat();
            
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;
            bkgWorker.RunWorkerAsync();
        }

        public void CancelAllRequests()
        {
            searchCircleRequest = false;
            searchQRRequest = false;
            searchTemplateRequest = false;
        }
                    

        

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public CircleSegment GetBestCircle()
        {
            //Best circle, referenced to ROI
            Console.WriteLine("Best Circle (in pixels): " + bestCircle.ToString());
            return bestCircle;
        }

        public List<Position3D> GetBestCircles()
        {
            return bestCircles;
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
            Settings.Focus = detector.Focus;
            Settings.IsManualFocus = detector.IsManualFocus;
            Settings.CircleDetectorP1 = detector.Param1;
            Settings.CircleDetectorP2 = detector.Param2;
            Settings.BinaryThreshold = detector.Threshold;

            circleDetector = detector;
            bestCircles.Clear();
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

        public List<Position3D> GetTemplateMatches()
        {
            return searchTemplateResults;
        }

        public bool IsTemplateSearchActive()
        {
            return searchTemplateRequest;
        }

        public void RequestTemplateSearch(Mat template, OpenCvSharp.Rect roi, CameraSettings settings)
        {
            /*-------------------------------------------------------------------------
             * Entry point for performing a template-based search
             * roi is the area you wanna search - usually the full image
             *------------------------------------------------------------------------*/
                       
            Settings = settings.Clone();
            Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            searchTemplateROI = roi;
            searchTemplateRequest = true;
     
        }

        private bool FindCircleInROI()
        {
            /****************************************************************************
             * Private function for finding circles.
             * Returns false if no circle found, sets BestCircle to 0,0 r0
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
            int minR = (int)((circleDetector.Radius / 2) / scale.xScale);
            int maxR = (int)((circleDetector.Radius * 2) / scale.xScale);
            //double param1 = machine.SelectedPickTool.UpperCircleDetector.Param1;
            //double param2 = machine.SelectedPickTool.UpperCircleDetector.Param2;
           
            //Set Min distance between matches
            int minDist = maxR;

            // Find circles using HoughCircles
            Circles = Cv2.HoughCircles(
                CircleROI,
                circleDetector.DetectorType,        // Usually HoughModes.Gradient or HoughModes.GradientAlt
                dp: 1.5,
                minDist: minDist,                   //Was 800, formally set to 2xmin raduis
               // param1: param1,                     //smaller means more false circles
               // param2: param2,                     //smaller means more false circles
                minRadius: minR,            
                maxRadius: maxR);
            
            if (Circles.Length < circleDetector.CountPerScene)
            {
                Console.WriteLine("<CountPerScene (" + Circles.Length + "/" + circleDetector.CountPerScene + "). minR/maxR (pix): " + minR + "/" + maxR + "@" + scale.xScale);
                OpenCvSharp.Cv2.Rectangle(ColorImage, circleDetector.ROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
                return false;
            }
            else
            {
                // Calculate the center of the image, in pixels
                Point2f roiCenter = new Point2f(CircleROI.Width / 2.0f, CircleROI.Height / 2.0f);
                // Sort by center or image
                bestCircle = Circles.OrderBy(circle => Math.Pow(circle.Center.X - roiCenter.X, 2) + Math.Pow(circle.Center.Y - roiCenter.Y, 2)).First();
                bestCircle.Center = new Point2f(bestCircle.Center.X - roiCenter.X, bestCircle.Center.Y - roiCenter.Y);
                Console.WriteLine("Found: Best Circle minR/avg/maxR (pix): " + minR + "/" + bestCircle.Radius + "/" + maxR + "@" + scale.xScale);
                for (int i = 0; i < circleDetector.CountPerScene; i++)
                {
                    Position3D cir = new Position3D(Circles.ElementAt(i));
                    cir.X -= roiCenter.X; cir.Y -= roiCenter.Y; // Correct for ROI within the frame
                    cir.Z = circleDetector.zEstimate;           // Store the Z of the target
                    bestCircles.Add(cir);
                }
                
            }
            OpenCvSharp.Cv2.Rectangle(ColorImage, circleDetector.ROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
            return true;
        }

        private bool FindTemplateInImage()
        {
            /****************************************************************************
             * Private function for things based on a Template
             * Returns false if no matches found.
             * Returns true if matches found.  List of Position3D is created.
             * from the center of the image ROI in pixels.
             * 
             *****************************************************************************/

            // Extract the region of interest (ROI) from the input image
            Mat roiImage = new Mat(GrayImage, searchTemplateROI);

            // Preprocess the ROI using adaptive thresholding
            Mat thresholdedRoiImage = new Mat();
            Cv2.AdaptiveThreshold(roiImage, thresholdedRoiImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 5);

            // Preprocess the template image using adaptive thresholding
            Mat thresholdedTemplateImage = new Mat();
            Cv2.AdaptiveThreshold(grayTemplateImage, thresholdedTemplateImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 5);

            // Perform template matching
            Mat matchResult = new Mat();
            try
            {
                Cv2.MatchTemplate(thresholdedRoiImage, thresholdedTemplateImage, matchResult, TemplateMatchModes.CCoeffNormed);
            }catch(Exception e) { return false; }

            // Threshold to determine "good matches"
            searchTemplateResults.Clear();
            double threshold = 0.2; // Adjust based on requirements (closer to 1.0 for stricter matches)
            while (true)
            {
                // Find the location of the maximum match
                Cv2.MinMaxLoc(matchResult, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                // If the maximum match value is below the threshold, exit the loop
                if (maxVal < threshold)
                    break;

                // Add the matching region to the results list
                int x = maxLoc.X + searchTemplateROI.X;
                int y = maxLoc.Y + searchTemplateROI.Y;
                searchTemplateResults.Add(new Position3D { X = x, Y = y, Width = grayTemplateImage.Cols, Height = grayTemplateImage.Rows });

                // Suppress the matched region in the result matrix to find distinct matches - connected +/- 10
                OpenCvSharp.Rect box;
                Cv2.FloodFill(matchResult, maxLoc, new Scalar(0), out box, new Scalar(10), new Scalar(10), FloodFillFlags.Link8);

            }

            if (searchTemplateResults.Count > 0)
                return true;
            return false; 
    
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
                Feeder feeder = machine.selectedCassette.selectedFeeder;
                double partZLocation = Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE + machine.Cal.CalPad.Z;
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
                    Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
                    Cv2.FloodFill(thresImage, maxloc, 0); //mark drawn blob so we don't find it again
                    match_count++;
                }
                else if (match_count > 0 && feeder != null)
                {
                    part.IsInView = true;
                    // Tell Part where to pick the next part
                    x_next += partROI.X;
                    y_next += partROI.Y;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Update next part location & calculate pick location
                        feeder.SetCandidateNextPartPickLocation(x_next, y_next, partZLocation, 0);
                    });
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
                                   
                    RawImage = Capture.RetrieveMat();
                    Cv2.CopyTo(RawImage, ColorImage);
                    Cv2.CvtColor(RawImage, GrayImage, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(GrayImage, GrayImage, new OpenCvSharp.Size(5, 5), 0);
                    Cv2.Threshold(GrayImage, ThresImage, Settings.BinaryThreshold, 255, ThresholdTypes.Binary);  // Default is 80
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
                            // Add 'break' here to just show the first circle in the list
                        }
                        if(bestCircles.Count >= (circleDetector.ScenesToAquire * circleDetector.CountPerScene))
                            searchCircleRequest = false;
                    }
                }
                else if (searchTemplateRequest)
                {
                    if (FindTemplateInImage())
                    {
                        searchTemplateRequest = false;
                        foreach (Position3D pos in searchTemplateResults)
                        {
                            Cv2.Rectangle(ColorImage, pos.GetRect(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                            Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(pos.GetRect().X + (pos.GetRect().Width / 2), pos.GetRect().Y + (pos.GetRect().Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
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
