using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using Picky.Properties;
using System;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Windows.Navigation;
using static Picky.MachineMessage;

namespace Picky
{
    public class CalibrationModel : INotifyPropertyChanged
    {
        private CalTargetModel target = new CalTargetModel ();
        public CalTargetModel Target
        {
            get { return target; }
            set { target = value; OnPropertyChanged(nameof(Target)); }
        }

        /* FeederModel/Cassette */
        private Position3D qRRegion = new Position3D(Constants.TRAVEL_LIMIT_X_MM, 200.0, 0, Constants.TRAVEL_LIMIT_X_MM, 10.0);
        public Position3D QRRegion
        {
            get { return qRRegion; }
            set { qRRegion = value; OnPropertyChanged(nameof(QRRegion)); }
        }
        private Position3D channelRegion = new Position3D(Constants.TRAVEL_LIMIT_X_MM, 210.0, 0, Constants.TRAVEL_LIMIT_X_MM, Constants.TRAVEL_LIMIT_X_MM - 210.0);
        public Position3D ChannelRegion
        {
            get { return channelRegion; }
            set { channelRegion = value; OnPropertyChanged(nameof(ChannelRegion)); }
        }

        public CameraSettings QRCaptureSettings { get; set; }
        public CameraSettings ChannelCaptureSettings { get; set; }


        /* Used to calculate mm/pixel and different Z using similar triangles */
        private Position3D mmPerPixUpper = new Position3D(Constants.DEFAULT_MM_PER_PIXEL, Constants.DEFAULT_MM_PER_PIXEL);
        public Position3D MMPerPixUpper
        {
            get { return mmPerPixUpper; }
            set { mmPerPixUpper = value; OnPropertyChanged(nameof(MMPerPixUpper)); }
        }

        private Position3D mmPerPixLower = new Position3D(Constants.DEFAULT_MM_PER_PIXEL, Constants.DEFAULT_MM_PER_PIXEL);
        public Position3D MMPerPixLower
        {
            get { return mmPerPixLower; }
            set { mmPerPixLower = value; OnPropertyChanged(nameof(MMPerPixLower)); }
        }

        /* Steps Per Unit */
        private double stepsPerUnitX;
        public double StepsPerUnitX
        {
            get { return stepsPerUnitX; }
            set { stepsPerUnitX = value; OnPropertyChanged(nameof(StepsPerUnitX)); }
        }

        private double stepsPerUnitY;
        public double StepsPerUnitY
        {
            get { return stepsPerUnitY; }
            set { stepsPerUnitY = value; OnPropertyChanged(nameof(StepsPerUnitY)); }
        }

        private double stepsPerUnitZ;
        public double StepsPerUnitZ
        {
            get { return stepsPerUnitZ; }
            set { stepsPerUnitZ = value; OnPropertyChanged(nameof(StepsPerUnitZ)); }
        }


        private double calculatedStepsPerUnitX;
        public double CalculatedStepsPerUnitX
        {
            get { return calculatedStepsPerUnitX; }
            set { calculatedStepsPerUnitX = value; OnPropertyChanged(nameof(CalculatedStepsPerUnitX)); }
        }

        private double calculatedStepsPerUnitY;
        public double CalculatedStepsPerUnitY
        {
            get { return calculatedStepsPerUnitY; }
            set { calculatedStepsPerUnitY = value; OnPropertyChanged(nameof(CalculatedStepsPerUnitY)); }
        }

        private double calculatedStepsPerUnitZ;
        public double CalculatedStepsPerUnitZ
        {
            get { return calculatedStepsPerUnitZ; }
            set { calculatedStepsPerUnitZ = value; OnPropertyChanged(nameof(CalculatedStepsPerUnitZ)); }
        }

        [JsonIgnore]
        private bool isPreviewUpperTargetActive = false;
        [JsonIgnore]
        public bool IsPreviewUpperTargetActive
        {
            get { return isPreviewUpperTargetActive; }
            set {
                MachineModel machine = MachineModel.Instance;
                machine.downCamera.IsTemplatePreviewActive = value; 
                isPreviewUpperTargetActive = value;
                if (value)
                {
                    isPreviewLowerTargetActive = false; isPreviewGridActive = false;
                    Target.QueueCalTargetSearch(Target.UpperTemplateFileName, Target.ActualLocUpper, Target.upperSettings, null, 5.5);
                }
                OnPropertyChanged(nameof(IsPreviewUpperTargetActive)); }
        }

        [JsonIgnore]
        private bool isPreviewLowerTargetActive = false;
        [JsonIgnore]
        public bool IsPreviewLowerTargetActive
        {
            get { return isPreviewLowerTargetActive; }
            set {
                MachineModel machine = MachineModel.Instance;
                machine.downCamera.IsTemplatePreviewActive = value;
                isPreviewLowerTargetActive = value;
                if (value)
                {
                    isPreviewUpperTargetActive = false; isPreviewGridActive = false;
                    Target.QueueCalTargetSearch(Target.LowerTemplateFileName, Target.ActualLocLower, Target.lowerSettings, null, 5);
                }
                OnPropertyChanged(nameof(IsPreviewLowerTargetActive)); }
        }

        [JsonIgnore]
        private bool isPreviewGridActive = false;
        [JsonIgnore]
        public bool IsPreviewGridActive
        {
            get { return isPreviewGridActive; }
            set {
                MachineModel machine = MachineModel.Instance;
                machine.downCamera.IsTemplatePreviewActive = value;
                isPreviewGridActive = value;
                if (value)
                {
                    Target.QueueCalTargetSearch(Target.GridTemplateFileName, Target.GridOrigin, Target.gridSettings, null, 2);
                    isPreviewLowerTargetActive = false; isPreviewUpperTargetActive = false;
                }
                OnPropertyChanged(nameof(IsPreviewGridActive));
            }
        }


        /*Calculated */
        public double DownCameraToItemX { get; set; }
        public double DownCameraToItemY { get; set; }
               
        private double downCameraToPickHeadX;
        public double PickHeadToCameraX 
        {
            get { return downCameraToPickHeadX; }
            set { downCameraToPickHeadX = value; OnPropertyChanged(nameof(PickHeadToCameraX)); }
        }

        private double downCameraToPickHeadY;
        public double PickHeadToCameraY
        {
            get { return downCameraToPickHeadY; }
            set { downCameraToPickHeadY = value; OnPropertyChanged(nameof(PickHeadToCameraY)); }
        }
        
        private Position3D deckPad = new Position3D { X = Constants.ZPROBE_CAL_DECK_PAD_X, Y = Constants.ZPROBE_CAL_DECK_PAD_Y }; 
        public Position3D DeckPad
        {
            get { return deckPad; }
            set { deckPad = value; OnPropertyChanged(nameof(DeckPad)); }
        }

        private Position3D calPad = new Position3D { X = Constants.ZPROBE_CAL_PAD_X, Y = Constants.ZPROBE_CAL_PAD_Y };
        public Position3D CalPad
        {
            get { return calPad; }
            set { calPad = value; OnPropertyChanged(nameof(CalPad)); }
        }
                
        public CalibrationModel()
        {
            // Create Target
            Target = new CalTargetModel();
            
            QRCaptureSettings = new CameraSettings();
            ChannelCaptureSettings = new CameraSettings();
        }
            

        /* Calculate Values Based on Calibration */

        public (double xScale, double yScale) GetScaleMMPerPixAtZ(double z) {
            /*----------------------------------------------------------------------------------
             - Returns the scale in mm/pix at the given Z (in mm) requires the calibration has 
             - been performed. Uses linear interpolation.  z is physical distance camera-to-item
             - WITHOUT A TIP!
             -----------------------------------------------------------------------------------*/

            double scaleFactor = (z - MMPerPixUpper.Z) / (MMPerPixLower.Z - MMPerPixUpper.Z);
            double mmPerPixX = MMPerPixUpper.X + scaleFactor * (MMPerPixLower.X - MMPerPixUpper.X);
            double mmPerPixY = MMPerPixUpper.Y + scaleFactor * (MMPerPixLower.Y - MMPerPixUpper.Y);
         
            return (mmPerPixX, mmPerPixY);
        }

        public OpenCvSharp.Rect GetQRCodeROI()
        {
            /*------------------------------------------------------------------------------------
             * Returns the roi, in pixels based on current position and defined QR code region.
             * The defined region and the current position is in pixels.  If the pixel calculation
             * is beyond the limits of the frame the Rect is bounded at the frame edge.  Also uses
             * scale at the given Z.  Yes, lots going on here!
             * -----------------------------------------------------------------------------------*/

            MachineModel machine = MachineModel.Instance;
            var scale = GetScaleMMPerPixAtZ(QRRegion.Z);

            int x = 0;

            double y_mm = machine.CurrentY - QRRegion.Y;
            double y_pix = y_mm / scale.yScale;
            int y = (y_pix > Constants.CAMERA_FRAME_HEIGHT) ? 0 : (int)((Constants.CAMERA_FRAME_HEIGHT / 2 ) - y_pix);

            int width = Constants.CAMERA_FRAME_WIDTH;

            double height_mm = Constants.QR_CODE_SIZE_MM;
            double height_pix = height_mm / scale.yScale;
            int height = ((height_pix + y) > Constants.CAMERA_FRAME_HEIGHT) ? Constants.CAMERA_FRAME_HEIGHT - y : (int)height_pix;

            OpenCvSharp.Rect rect = new Rect(x, y, width, height);
            //Console.WriteLine("QR ROI (px): " + rect.ToString());

            return rect;
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
