using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.ComponentModel;


namespace Picky
{
    public class CalibrationModel : INotifyPropertyChanged
    {


        /* Resolution - PCB */
        private double pcbMMToPixX;
        public double PcbMMToPixX
        {
            get { return pcbMMToPixX; }
            set { pcbMMToPixX = value; OnPropertyChanged(nameof(PcbMMToPixX)); }
        }
        private double pcbMMToPixY;
        public double PcbMMToPixY
        {
            get { return pcbMMToPixY; }
            set { pcbMMToPixY = value; OnPropertyChanged(nameof(PcbMMToPixY)); }
        }
        private double pcbZHeight;
        public double PcbZHeight
        {
            get { return pcbZHeight; }
            set { pcbZHeight = value; OnPropertyChanged(nameof(PcbZHeight)); }
        }

        /* Resolution - Tool */
        private double toolMMToPixX;
        public double ToolMMToPixX
        {
            get { return toolMMToPixX; }
            set { toolMMToPixX = value; OnPropertyChanged(nameof(ToolMMToPixX)); }
        }
        private double toolMMToPixY;
        public double ToolMMToPixY
        {
            get { return toolMMToPixY; }
            set { toolMMToPixY = value; OnPropertyChanged(nameof(ToolMMToPixY)); }
        }
        private double toolZHeight;
        public double ToolZHeight
        {
            get { return toolZHeight; }
            set { toolZHeight = value; OnPropertyChanged(nameof(ToolZHeight)); }
        }

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

        private double feederMMToPixX;
        public double FeederMMToPixX
        {
            get { return feederMMToPixX; }
            set { feederMMToPixX = value; OnPropertyChanged(nameof(FeederMMToPixX)); }
        }
        private double feederMMToPixY;
        public double FeederMMToPixY
        {
            get { return feederMMToPixY; }
            set { feederMMToPixY = value; OnPropertyChanged(nameof(FeederMMToPixY)); }
        }
        private double feederZHeight;
        public double FeederZHeight
        {
            get { return feederZHeight; }
            set { feederZHeight = value; OnPropertyChanged(nameof(FeederZHeight)); }
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
        public double StepsPerUnitX { get; set; }
        public double StepsPerUnitY { get; set; }

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

        public double DownCameraAngleX { get; set; }
        public double DownCameraAngleY { get; set; }

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

        }

        /* Calculate Values Based on Settings */
        public (double x_offset, double y_offset) GetPickHeadOffsetToCamera(double targetZ)
        /*************************************************************************************
         * When you've centered the camera and want to pick something you need to call here 
         * with the approximate z of the target.  This function will return the offset.  After you 
         * pick the item you need to subtract the offset.  Do not call with the upward z.  This
         * is a downward z only.
         * 
         ****/
        {
            DownCameraToPickHeadX = -1 * (MachineOriginToPickHeadX1 - MachineOriginToDownCameraX + (Math.Sin(DownCameraAngleX) * targetZ));
            DownCameraToPickHeadY = -1 * (MachineOriginToPickHeadY1 - MachineOriginToDownCameraY + (Math.Sin(DownCameraAngleY) * targetZ));
            return (DownCameraToPickHeadX, DownCameraToPickHeadY);
        }
                
        private bool calcDownAngles()
        {
            /****************************************************************************************
             * Update calculation angles.  Don't call directly
             * 
             *****/

            double dist = MachineOriginToPickHeadZ1 - MachineOriginToPickHeadZ2;
            if (dist == 0)
            {
                Console.WriteLine("Calibration Error: Z axis distances is zero");
                return false;
            }
            DownCameraAngleX = Math.Atan((MachineOriginToPickHeadX1 - MachineOriginToPickHeadX2) / (dist));
            DownCameraAngleY = Math.Atan((MachineOriginToPickHeadY1 - MachineOriginToPickHeadY2) / (dist));
            Console.WriteLine("Down Angles Updated. DownCameraAngleX: " + DownCameraAngleX + " Y: " + DownCameraAngleY);
            return true;
        }


        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
