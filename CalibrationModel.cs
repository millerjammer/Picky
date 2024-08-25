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
        public double MachineOriginToDownCameraX { get; set; }
        public double MachineOriginToDownCameraY { get; set; }
        public double MachineOriginToDownCameraZ { get; set; }

        private double machineOriginToPickHeadX1;
        public double MachineOriginToPickHeadX1 
        {  
            get { return machineOriginToPickHeadX1; }
            set { machineOriginToPickHeadX1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadX1)); }
        }

        private double machineOriginToPickHeadY1;
        public double MachineOriginToPickHeadY1
        {
            get { return machineOriginToPickHeadY1; }
            set { machineOriginToPickHeadY1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadY1)); }
        }

        private double machineOriginToPickHeadZ1;
        public double MachineOriginToPickHeadZ1
        {
            get { return machineOriginToPickHeadZ1; }
            set { machineOriginToPickHeadZ1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadZ1)); }
        }

        private double machineOriginToPickHeadX2;
        public double MachineOriginToPickHeadX2
        {
            get { return machineOriginToPickHeadX2; }
            set { machineOriginToPickHeadX2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadX2)); }
        }

        private double machineOriginToPickHeadY2;
        public double MachineOriginToPickHeadY2
        {
            get { return machineOriginToPickHeadY2; }
            set { machineOriginToPickHeadY2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadY2)); }
        }

        private double machineOriginToPickHeadZ2;
        public double MachineOriginToPickHeadZ2
        {
            get { return machineOriginToPickHeadZ2; }
            set { machineOriginToPickHeadZ2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadZ2)); }
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
        public double DownCameraToPickHeadX 
        {
            get { return downCameraToPickHeadX; }
            set { downCameraToPickHeadX = value; OnPropertyChanged(nameof(DownCameraToPickHeadX)); }
        }

        private double downCameraToPickHeadY;
        public double DownCameraToPickHeadY
        {
            get { return downCameraToPickHeadY; }
            set { downCameraToPickHeadY = value; OnPropertyChanged(nameof(DownCameraToPickHeadY)); }
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
         - with the approximate z of the Target.  This function will return the offset.  After you 
         - pick the item you need to subtract the offset.  Do not call with the upward z.  This
         - is a downward z only. The target z should be the actual z from camera to surface
         --------------------------------------------------------------------------------------*/
        {
            double slope_x = (MachineOriginToPickHeadX1 - MachineOriginToPickHeadX2) / (MachineOriginToPickHeadZ1 - MachineOriginToPickHeadZ2);
            double slope_y = (MachineOriginToPickHeadY1 - MachineOriginToPickHeadY2) / (MachineOriginToPickHeadZ1 - MachineOriginToPickHeadZ2);
           
            double offset_x = MachineOriginToPickHeadX2 + (slope_x * (targetZ - MachineOriginToPickHeadZ2));
            double offset_y = MachineOriginToPickHeadY2 + (slope_y * (targetZ - MachineOriginToPickHeadZ2));

            DownCameraToPickHeadX = (MachineOriginToDownCameraX - offset_x);
            DownCameraToPickHeadY = (MachineOriginToDownCameraY + offset_y);

            return (DownCameraToPickHeadX, DownCameraToPickHeadY);
        }
        
                       
        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
