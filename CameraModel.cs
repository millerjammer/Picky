﻿using EnvDTE90;
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
             

        /* Mats for general availability */
        public Mat CaptureImage { get; set; }
        public Mat ColorImage { get; set; }     //This is the mark-up image
        public Mat GrayImage { get; set; }
        public Mat ThresImage { get; set; }
        public Mat EdgeImage { get; set; }
        public Mat DilatedImage { get; set; }
        public Mat QRDetectImage { get; set; }
        public Mat MatchImage { get; set; }
        public Mat MatchThresholdImage { get; set; }
        public Mat SearchTemplate { get; set; }

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

        public Part PartToFind { get; set; }
        
        /* Template Request */
        private bool searchTemplateRequest = false;
        private OpenCvSharp.Rect searchTemplateROI;
        private List<Position3D> searchTemplateResults = new List<Position3D>();
        public bool IsTemplatePreviewActive { get; set; } = false;
               
        /* QR Request */
        private readonly object _qrZoneResultsLock = new object();
        private List<(string str, OpenCvSharp.Rect pos)> qrZoneResults = new List<(string str, OpenCvSharp.Rect pos)>();

        public bool requestQRDecode = false;
        public bool RequestQRDecode
        {
            get { return requestQRDecode; }
            set { requestQRDecode = value; if(value == true) Settings = machine.Cal.QRCaptureSettings.Clone(); OnPropertyChanged(nameof(RequestQRDecode)); }
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
            set { settings = value; Console.WriteLine("Set: Camera Settings"); settings.ApplySettings(this); OnPropertyChanged(nameof(Settings)); } //Notify listeners
        }

        public Mat selectedViewMat { get; set; }
        public VideoCapture Capture { get; set; }

        private readonly BackgroundWorker bkgWorker;

        public CameraModel(int cameraIndex, MachineModel mm)
        {
            machine = mm;
            CaptureImage = new Mat();
            ColorImage = new Mat();
            GrayImage = new Mat();
            ThresImage = new Mat();
            EdgeImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            DilatedImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            QRDetectImage = new Mat();
            MatchImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            MatchThresholdImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);

            Settings = new CameraSettings();
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
            searchTemplateRequest = false;
            RequestQRDecode = false;
        }


        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    OpenCvSharp.Rect[] qrs = Array.ConvertAll(rects, rect => new OpenCvSharp.Rect((int)(rect.X / .3) + search_roi.X, (int)(rect.Y / .3) + search_roi.Y, (int)(rect.Width / .3), (int)(rect.Height / .3)));
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

            SearchTemplate = template;
            searchTemplateROI = roi;
            searchTemplateRequest = true;

        }
          

        private bool FindTemplateInImage(Mat template, OpenCvSharp.Rect search_roi, List<Position3D> search_result)
        {
            /****************************************************************************
             * Private function for things based on a Template
             * Returns false if no matches found. Result in pixel, referenced to full frame
             * Returns true if matches found.  List of Position3D is created.
             * x, y, in pixels measured from the 0,0 of the camera frame to center of match
             * circle. Width and height match the template dimensions.  If you want an 
             * offset you'll need to subtract half the frame.
             *****************************************************************************/

            Mat roiImage = new Mat();
            Mat mImage = new Mat();
            Mat res_32f;
            double x, y;

            try
            {
                // Convert Template to Gray, Resize Gray Image to ROI and create Match Image.
                Cv2.CvtColor(template, grayTemplateImage, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                roiImage = new Mat(GrayImage, search_roi);
                res_32f = new Mat(roiImage.Rows - template.Rows + 1, roiImage.Cols - template.Cols + 1, MatType.CV_32FC1);
                Cv2.MatchTemplate(roiImage, grayTemplateImage, res_32f, TemplateMatchModes.CCoeffNormed);
            }
            catch (Exception e) { 
                return false; 
            };

            /* Show Results - search area only */
            res_32f.ConvertTo(MatchImage, MatType.CV_8U, 255.0);
            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            CameraSettings set = machine.downCamera.Settings;
            mImage = new Mat();
            Cv2.AdaptiveThreshold(MatchImage, mImage, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, set.TemplateThreshold);

            // Perform morphology - reducing image size seems to be the best followed by .Open
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(10, 10)); // rectangular kernel
            Cv2.MorphologyEx(mImage, MatchThresholdImage, MorphTypes.Dilate, kernel);


            search_result.Clear();
            CircleSegment[] circles = Cv2.HoughCircles(
                MatchThresholdImage,
                HoughModes.GradientAlt,
                dp: 1,           // Inverse ratio of accumulator resolution
                minDist: 30,     // Minimum distance between circle centers
                param1: set.CircleDetectorP1,     // Higher threshold for the Canny edge detector
                param2: set.CircleDetectorP2,      // Accumulator threshold for circle detection
                minRadius: 5,    // Minimum circle radius (10px diameter → radius = 5px)
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
                        CaptureImage = Capture.RetrieveMat();
                    } while (CaptureImage.Cols != Constants.CAMERA_FRAME_WIDTH);
                    Cv2.CopyTo(CaptureImage, ColorImage);
                    Cv2.CvtColor(CaptureImage, GrayImage, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(GrayImage, GrayImage, new OpenCvSharp.Size(5, 5), 0);
                    Cv2.Threshold(GrayImage, ThresImage, Settings.BinaryThreshold, 255, ThresholdTypes.Binary);
                    Cv2.Canny(ThresImage, EdgeImage, 50, 150, 3);  //50 150 3
                    Cv2.Dilate(EdgeImage, DilatedImage, null, iterations: 2);
                }
                catch (Exception ex)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                if (searchTemplateRequest)
                {
                    if (FindTemplateInImage(SearchTemplate, searchTemplateROI, searchTemplateResults))
                    {
                        if (!machine.Cal.IsPreviewLowerTargetActive && !machine.Cal.IsPreviewUpperTargetActive && !machine.Cal.IsPreviewGridActive)
                            searchTemplateRequest = false;
                        foreach (Position3D pos in searchTemplateResults)
                        {
                            Cv2.Rectangle(ColorImage, pos.GetRect(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                            Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(pos.GetRect().X + (pos.GetRect().Width / 2), pos.GetRect().Y + (pos.GetRect().Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
                        }
                    }
                }
                else if (machine.Region == MachineModel.PickHeadRegion.FeederQR && RequestQRDecode)
                {
                    Cv2.Rectangle(ColorImage, machine.Cal.GetQRCodeROI(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    if (DecodeQRCode(machine.Cal.GetQRCodeROI()))
                    {
                        for (int i = 0; i < qrZoneResults.Count; i++)
                            Cv2.Rectangle(ColorImage, qrZoneResults[i].pos, new OpenCvSharp.Scalar(0, 255, 0), 4);
                    }
                }
                else if (machine.Region == MachineModel.PickHeadRegion.FeederQR || machine.Region == MachineModel.PickHeadRegion.FeederPick) { 
                    if (machine?.SelectedCassette?.SelectedFeeder != null && machine.IsMachineInMotion == false)
                    {
                        /* Draw the pick ROI! We're in the region, not in motion and a feeder is selected */
                        Position3D pickROI = machine.SelectedCassette.SelectedFeeder.GetPickROI();
                        OpenCvSharp.Rect rect = TranslationUtils.ConvertGlobalMMRectToFrameRectPix(pickROI);
                        if (machine.SelectedCassette.SelectedFeeder.Part != null && rect != default)
                        {
                            Cv2.Rectangle(ColorImage, rect, new OpenCvSharp.Scalar(255, 0, 0), 4);
                            /* Find Parts */
                            if (FindTemplateInImage(machine.SelectedCassette.SelectedFeeder.Part.TemplateMat, rect, searchTemplateResults))
                            {
                                /* Update data for the next part to pick */
                                Position3D pos_mm = TranslationUtils.ConvertFrameRectPosPixToGlobalMM(searchTemplateResults.OrderBy(pos => pos.Y).FirstOrDefault(), pickROI.Z);
                                machine.SelectedCassette.SelectedFeeder.NextPartOpticalLocation = pos_mm;
                                /* Draw Parts! */
                                foreach (Position3D pos in searchTemplateResults)
                                {                                   
                                    Cv2.Rectangle(ColorImage, pos.GetRect(), new OpenCvSharp.Scalar(0, 255, 0), 2);
                                    Cv2.DrawMarker(ColorImage, new OpenCvSharp.Point(pos.GetRect().X + (pos.GetRect().Width / 2), pos.GetRect().Y + (pos.GetRect().Height / 2)), Scalar.DarkOrange, OpenCvSharp.MarkerTypes.Cross, 50, 1);
                                }
                            }
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
