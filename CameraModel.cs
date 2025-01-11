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
using Xamarin.Forms.Xaml;

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
        public Mat QRDetectImage { get; set; }
        public Mat MatchImage { get; set; }
        public Mat MatchThresholdImage { get; set; }
        public Mat searchTemplate { get; set; }

        public Mat CircleROI { get; set; }
        public Mat RectangleROI { get; set; }
        public Mat QRImageROI { get; set; }
        

        /* What we are currently viewing */
        public VisualizationStyle SelectedVisualizationViewItem { get; set; }

        /* What we are currently viewing */
        public ImageProcessingStyle SelectedImagrProcessingStyle { get; set; }

        /* Mats for Part/Template Identification */
        private Mat grayTemplateImage;
        private Mat matchResultImage;

        private Point2d nextPartOffset;
        public Part PartToFind { get; set; }
        private List<Position3D> searchPartResults = new List<Position3D>();
        
        /* Template Request */
        private bool searchTemplateRequest = false;
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
        private readonly object _qrZoneResultsLock = new object();
        private List<(string str, OpenCvSharp.Rect pos)> qrZoneResults = new List<(string str, OpenCvSharp.Rect pos)>();
          

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
            set { settings = value; Console.WriteLine("Camera Settings Upper Changed"); Settings.ApplySettings(this); OnPropertyChanged(nameof(Settings)); } //Notify listeners
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
            QRDetectImage = new Mat();
            MatchImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            MatchThresholdImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);

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

        /*--------------------------------- QR Code ---------------------------------------*/

        public List<(string str, OpenCvSharp.Rect pos)> GetQrZoneResults()
        {
            /*-------------------------------------------------------------------
             * This is a thread-safe method of accessing visible QR codes 
             *
             *------------------------------------------------------------------*/
            lock (_qrZoneResultsLock)
            {
                return new List<(string str, OpenCvSharp.Rect pos)>(qrZoneResults);
            }
        }
        
        private bool DecodeQRCode(OpenCvSharp.Rect search_roi)
        {
            /*---------------------------------------------------------------------
             * Private function for decoding QR codes
             * By experimentation reduction to .3 is optimal
             * 
             *--------------------------------------------------------------------*/

            Point2f[] currentQRCodePoints;
            try
            {
                using (var detector = new QRCodeDetector())
                {
                    Mat iQRImageROI = new Mat(ThresImage, search_roi);

                    Mat resizedImage = new Mat();
                    Cv2.Resize(iQRImageROI, resizedImage, new OpenCvSharp.Size(0, 0), 0.3, 0.3);

                    // Perform morphology - reducing image size seems to be the best followed by .Open
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)); // 3x3 rectangular kernel
                    Cv2.MorphologyEx(resizedImage, QRDetectImage, MorphTypes.Open, kernel);

                    detector.DetectMulti(QRDetectImage, out currentQRCodePoints);
                    lock (_qrZoneResultsLock)
                    {
                        qrZoneResults.Clear();
                    }
                    if (currentQRCodePoints.Length <= 0)
                        return false;
                    
                    detector.DecodeMulti(QRDetectImage, currentQRCodePoints, out string[] decodedTexts);
                    OpenCvSharp.Rect[] rects = QRCodeUtils.ConvertPointsToRects(currentQRCodePoints);
                    // Add the search_roi to return an array of rect referenced to the full frame
                    OpenCvSharp.Rect[] qrs = Array.ConvertAll(rects, rect => new OpenCvSharp.Rect((int)(rect.X /.3) + search_roi.X, (int)(rect.Y / .3) + search_roi.Y, (int)(rect.Width / .3), (int)(rect.Height / .3) ));
                    // Store results
                    lock (_qrZoneResultsLock)
                    {
                        for (int i = 0; i < decodedTexts.Length; i++)
                            qrZoneResults.Add((decodedTexts[i], qrs[i]));   // Doesn't appear this will return nulls or zero location
                    }                    
                    if (qrZoneResults.Count > 0)
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

        public void RequestTemplateSearch(Mat template, OpenCvSharp.Rect roi)
        {
            /*-------------------------------------------------------------------------
             * Entry point for performing a template-based search
             * roi is the area you wanna search - usually the full image
             *------------------------------------------------------------------------*/
                       
            searchTemplate = template;
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
                    cir.Z = circleDetector.zEstimate;           // Store the Z of the template
                    bestCircles.Add(cir);
                }
                
            }
            OpenCvSharp.Cv2.Rectangle(ColorImage, circleDetector.ROI, new OpenCvSharp.Scalar(0, 255, 0), 4);
            return true;
        }

        private bool FindTemplateInImage(Mat template, OpenCvSharp.Rect search_roi, List<Position3D> search_result)
        {
            /****************************************************************************
             * Private function for things based on a Template
             * Returns false if no matches found.
             * Returns true if matches found.  List of Position3D is created.
             * x, y, in pixels measured from the 0,0 of the camera frame to center of match
             * circle. Width and height match the template dimensions.  If you want an 
             * offset you'll need to subtract half the frame.
             *****************************************************************************/

            Mat roiImage, res_32f;
            double x, y;

            try
            {
                // Convert Template to Gray, Resize Gray Image to ROI and create Match Image.
                Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                roiImage = new Mat(GrayImage, search_roi);
                res_32f = new Mat(roiImage.Rows - template.Rows + 1, roiImage.Cols - template.Cols + 1, MatType.CV_32FC1);
                Cv2.MatchTemplate(roiImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);
            }
            catch (Exception e) { return false; };

            /* Show Results - search area only */
            res_32f.ConvertTo(MatchImage, MatType.CV_8U, 255.0);
            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            CameraSettings set = machine.downCamera.Settings;
            Cv2.AdaptiveThreshold(MatchImage, MatchThresholdImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, set.TemplateThreshold);

            search_result.Clear();
            CircleSegment[] circles = Cv2.HoughCircles(
                MatchThresholdImage,
                HoughModes.GradientAlt,
                dp: 1,           // Inverse ratio of accumulator resolution
                minDist: 30,     // Minimum distance between circle centers
                param1: set.CircleDetectorP1,     // Higher threshold for the Canny edge detector
                param2: set.CircleDetectorP2,      // Accumulator threshold for circle detection
                minRadius: 10,    // Minimum circle radius (10px diameter → radius = 5px)
                maxRadius: 100     // Maximum circle radius
            );
            
            // Create a list of circles referenced to the search area 
            foreach (var circle in circles)
            {
                x = circle.Center.X + search_roi.X; y = circle.Center.Y + search_roi.Y;
                search_result.Add(new Position3D(x, y, 0, template.Width, template.Height));
            }
                     
            if (search_result.Count > 0)
                return true;
            return false; 
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
                    do
                    {
                        RawImage = Capture.RetrieveMat();
                    } while (RawImage.Cols != Constants.CAMERA_FRAME_WIDTH);
                    Cv2.CopyTo(RawImage, ColorImage);
                    Cv2.CvtColor(RawImage, GrayImage, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(GrayImage, GrayImage, new OpenCvSharp.Size(5, 5), 0);
                    Cv2.Threshold(GrayImage, ThresImage, Settings.BinaryThreshold, 255, ThresholdTypes.Binary);  
                    Cv2.Canny(ThresImage, EdgeImage, 50, 150, 3);  //50 150 3
                    Cv2.Dilate(EdgeImage, DilatedImage, null, iterations: 2);
                    
                    RawImage = null;  // hint garbage collection
                }
                catch (Exception ex)
                {
                   GC.Collect();
                   GC.WaitForPendingFinalizers();
                }

                if (searchCircleRequest)
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
                    if (FindTemplateInImage(searchTemplate, searchTemplateROI, searchTemplateResults))
                    {
                        
                        if(!machine.Cal.IsPreviewLowerTargetActive && !machine.Cal.IsPreviewUpperTargetActive && !machine.Cal.IsPreviewGridActive)
                            searchTemplateRequest = false;
                        foreach (Position3D pos in searchTemplateResults)
                        {
                            Cv2.Rectangle(ColorImage, pos.GetRect(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                            Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(pos.GetRect().X + (pos.GetRect().Width / 2), pos.GetRect().Y + (pos.GetRect().Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
                        }
                    }
                }
                else if (machine.IsMachineInQRRegion())
                {
                    Cv2.Rectangle(ColorImage, machine.Cal.GetQRCodeROI(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    if (DecodeQRCode(machine.Cal.GetQRCodeROI()))
                    {
                        for (int i = 0; i < qrZoneResults.Count; i++)
                        {
                            Cv2.Rectangle(ColorImage, qrZoneResults[i].pos, new OpenCvSharp.Scalar(0, 255, 0), 4);
                            //Console.WriteLine("QR: " + qrZoneResults.ElementAt(i).str);
                        }
                    }
                }
                else if (false)//PartToFind != null)
                {
                    OpenCvSharp.Rect partROI = machine.SelectedCassette.SelectedFeeder.GetPickROI().GetRect();
                    //if (FindTemplateInImage(PartToFind.Template, partROI, searchPartResults))
                    {
                        foreach (Position3D pos in searchTemplateResults)
                        {
                            Cv2.Rectangle(ColorImage, pos.GetRect(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                            Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(pos.GetRect().X + (pos.GetRect().Width / 2), pos.GetRect().Y + (pos.GetRect().Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
                        }
                    }
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
