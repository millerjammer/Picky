using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.ComponentModel;
using System.Windows.Navigation;

namespace Picky
{
    public class CalibrationModel : INotifyPropertyChanged
    {
        public CalResolutionTargetModel TargetResAtPCB { get; set; }
        public CalResolutionTargetModel TargetResAtTool { get; set; }

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
       
                
        /* Calculated based on similar triangles above */
        private double resolutionXAtZ;
        public double ResolutionXAtZ
        {
            get { return resolutionXAtZ; }
            set { resolutionXAtZ = value; OnPropertyChanged(nameof(ResolutionXAtZ)); }
        }
        private double resolutionYAtZ;
        public double ResolutionYAtZ
        {
            get { return resolutionYAtZ; }
            set { resolutionYAtZ = value; OnPropertyChanged(nameof(ResolutionYAtZ)); }
        }
        private double resolutionAtZ;
        public double ResolutionAtZ
        {
            get { return resolutionAtZ; }
            set { resolutionAtZ = value; OnPropertyChanged(nameof(ResolutionAtZ)); }
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
                
        private double zCalPadX = Constants.ZPROBE_CAL_PAD_X;
        public double ZCalPadX
        {
            get { return zCalPadX; }
            set { zCalPadX = value; OnPropertyChanged(nameof(ZCalPadX)); }
        }

        private double zCalPadY = Constants.ZPROBE_CAL_PAD_Y;
        public double ZCalPadY
        {
            get { return zCalPadY; }
            set { zCalPadY = value; OnPropertyChanged(nameof(ZCalPadY)); }
        }

        private double zCalPadZ;
        public double ZCalPadZ
        {
            get { return zCalPadZ; }
            set { zCalPadZ = value; OnPropertyChanged(nameof(ZCalPadZ)); }
        }


        public CalibrationModel()
        {
            CircleSegment targetCircle = new CircleSegment();
            targetCircle.Radius = (float)(CalTargetModel.TARGET_RESOLUTION_RADIUS_MILS * Constants.MIL_TO_MM);
            targetCircle.Center.X = (float)(CalTargetModel.TARGET_PCB_POS_X_MM);
            targetCircle.Center.Y = (float)(CalTargetModel.TARGET_PCB_POS_Y_MM);

            TargetResAtPCB = new CalResolutionTargetModel(targetCircle);
            
            
            targetCircle.Center.X = (float)(CalTargetModel.TARGET_TOOL_POS_X_MM);
            targetCircle.Center.Y = (float)(CalTargetModel.TARGET_TOOL_POS_Y_MM);
            TargetResAtTool =  new CalResolutionTargetModel(targetCircle);
           
        }

        /* Calculate Values Based on Calibration */

        public (double xScale, double yScale) GetScaleMMPerPixAtZ(double z) {
            /*----------------------------------------------------------------------------------
             - Returns the scale in mm/pix at the given Z (in mm) requires the calibration has 
             - been performed. Uses linear interpolation.  z is typically the plane of the tool tip
             -----------------------------------------------------------------------------------*/
            double slope_x = (TargetResAtPCB.MMPerPixX - TargetResAtTool.MMPerPixX) / (TargetResAtPCB.MMHeightZ - TargetResAtTool.MMHeightZ);
            double slope_y = (TargetResAtPCB.MMPerPixY - TargetResAtTool.MMPerPixY) / (TargetResAtPCB.MMHeightZ - TargetResAtTool.MMHeightZ);

            ResolutionXAtZ = TargetResAtTool.MMPerPixX + (slope_x * (z - TargetResAtTool.MMHeightZ));
            ResolutionYAtZ = TargetResAtTool.MMPerPixY + (slope_y * (z - TargetResAtTool.MMHeightZ));

            return (ResolutionXAtZ, ResolutionYAtZ);
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
