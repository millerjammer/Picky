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
        public CalTargetModel CalTarget { get; set; }

        /* Feeder */
        private double feeder0X;
        public double Feeder0X
        {
            get { return feeder0X; }
            set { feeder0X = value; OnPropertyChanged(nameof(Feeder0X)); }
        }
        private double feeder0Y;
        public double Feeder0Y
        {
            get { return feeder0Y; }
            set { feeder0Y = value; OnPropertyChanged(nameof(Feeder0Y)); }
        }

        private double feederNX;
        public double FeederNX
        {
            get { return feederNX; }
            set { feederNX = value; OnPropertyChanged(nameof(FeederNX)); }
        }
        private double feederNY;
        public double FeederNY
        {
            get { return feederNY; }
            set { feederNY = value; OnPropertyChanged(nameof(FeederNY)); }
        }
                       
        /* Used to calculate mm/pixel and different Z using similar triangles */
        private Position3D mmPerPixUpper;
        public Position3D MMPerPixUpper
        {
            get { return mmPerPixUpper; }
            set { mmPerPixUpper = value; OnPropertyChanged(nameof(MMPerPixUpper)); }
        }

        private Position3D mmPerPixLower;
        public Position3D MMPerPixLower
        {
            get { return mmPerPixLower; }
            set { mmPerPixLower = value; OnPropertyChanged(nameof(MMPerPixLower)); }
        }

        /* Camera/Pick Physics */
        private double originToUpCameraX;
        public double OriginToUpCameraX
        {
            get { return originToUpCameraX; }
            set { originToUpCameraX = value; OnPropertyChanged(nameof(originToUpCameraX)); }
        }
        private double originToUpCameraY;
        public double OriginToUpCameraY
        {
            get { return originToUpCameraY; }
            set { originToUpCameraY = value; OnPropertyChanged(nameof(originToUpCameraY)); }
        }

        private double originToDownCameraX;
        public double OriginToDownCameraX
        {
            get { return originToDownCameraX; }
            set { originToDownCameraX = value; OnPropertyChanged(nameof(originToDownCameraX)); }
        }
        private double originToDownCameraY;
        public double OriginToDownCameraY
        {
            get { return originToDownCameraY; }
            set { originToDownCameraY = value; OnPropertyChanged(nameof(originToDownCameraY)); }
        }

        private double originToPickHeadX1;
        public double OriginToPickHeadX1 
        {  
            get { return originToPickHeadX1; }
            set { originToPickHeadX1 = value; OnPropertyChanged(nameof(OriginToPickHeadX1)); }
        }

        private double originToPickHeadY1;
        public double OriginToPickHeadY1
        {
            get { return originToPickHeadY1; }
            set { originToPickHeadY1 = value; OnPropertyChanged(nameof(OriginToPickHeadY1)); }
        }

        private double originToPickHeadZ1;
        public double OriginToPickHeadZ1
        {
            get { return originToPickHeadZ1; }
            set { originToPickHeadZ1 = value; OnPropertyChanged(nameof(OriginToPickHeadZ1)); }
        }

        private double originToPickHeadX2;
        public double OriginToPickHeadX2
        {
            get { return originToPickHeadX2; }
            set { originToPickHeadX2 = value; OnPropertyChanged(nameof(OriginToPickHeadX2)); }
        }

        private double originToPickHeadY2;
        public double OriginToPickHeadY2
        {
            get { return originToPickHeadY2; }
            set { originToPickHeadY2 = value; OnPropertyChanged(nameof(OriginToPickHeadY2)); }
        }

        private double originToPickHeadZ2;
        public double OriginToPickHeadZ2
        {
            get { return originToPickHeadZ2; }
            set { originToPickHeadZ2 = value; OnPropertyChanged(nameof(OriginToPickHeadZ2)); }
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
                    isPreviewLowerTargetActive = false;
                    CalTarget.QueueCalTargetSearch(CalTarget.UpperTemplateFileName, CalTarget.ActualLocUpper, CalTarget.upperSettings, null, 5.5);
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
                    isPreviewUpperTargetActive = false;
                    CalTarget.QueueCalTargetSearch(CalTarget.LowerTemplateFileName, CalTarget.ActualLocLower, CalTarget.lowerSettings, null, 5);
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
                    CalTarget.QueueCalTargetSearch(CalTarget.GridTemplateFileName, CalTarget.Grid, CalTarget.gridSettings, null, 1.4);
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
            CalTarget = new CalTargetModel();
            mmPerPixUpper = new Position3D();
            mmPerPixLower = new Position3D();
        }
            

        /* Calculate Values Based on Calibration */

        public (double xScale, double yScale) GetScaleMMPerPixAtZ(double z) {
            /*----------------------------------------------------------------------------------
             - Returns the scale in mm/pix at the given Z (in mm) requires the calibration has 
             - been performed. Uses linear interpolation.  z is physical distance camera-to-item
             -----------------------------------------------------------------------------------*/

            double scaleFactor = (z - MMPerPixUpper.Z) / (MMPerPixLower.Z - MMPerPixUpper.Z);
            double mmPerPixX = MMPerPixUpper.X + scaleFactor * (MMPerPixLower.X - MMPerPixUpper.X);
            double mmPerPixY = MMPerPixUpper.Y + scaleFactor * (MMPerPixLower.Y - MMPerPixUpper.Y);
         
            return (mmPerPixX, mmPerPixY);
        }
          
        
        public (double x_offset, double y_offset) GetPickHeadOffsetToCameraAtZ(double targetZ)
        /*----------------------------------------------------------------------------------
         - When you've centered the camera and want to pick something you need to call here 
         - with the approximate z of the destination.  This function will return the offset.  After you 
         - pick the item you need to subtract the offset.  Why is this needed?  Because the probe
         - head may not be perfectly square to the surface - or the camera may not be square.
         - This is a simple slope-intercept in xy and z.  Slope is rise over run.    
         --------------------------------------------------------------------------------------*/
        {
            double slope_x = (OriginToPickHeadX2 - OriginToPickHeadX1) / (OriginToPickHeadZ2 - OriginToPickHeadZ1);
            double slope_y = (OriginToPickHeadY2 - OriginToPickHeadY1) / (OriginToPickHeadZ2 - OriginToPickHeadZ1);

            double offset_x = -(slope_x * targetZ) - OriginToPickHeadX1 + OriginToDownCameraX;
            double offset_y = -(slope_y * targetZ) - OriginToPickHeadY1 + OriginToDownCameraY;

            PickHeadToCameraX = offset_x;
            PickHeadToCameraY = offset_y;
            
            return (offset_x, offset_y);
        }
        
                       
        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
