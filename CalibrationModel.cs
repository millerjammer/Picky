using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.ComponentModel;


namespace Picky
{
    public class CalibrationModel : INotifyPropertyChanged
    {


        /* Resolution */
        public double PcbMMToPixX { get; set; }
        public double PcbMMToPixY { get; set; }
        public double PcbZHeight { get; set; }

        public double ToolMMToPixX { get; set; }
        public double ToolMMToPixY { get; set; }
        public double ToolZHeight { get; set; }

        public double FeederMMToPixX { get; set; }
        public double FeederMMToPixY { get; set; }
        public double FeederZHeight { get; set; }


        /* Camera/Pick Physics */
        public double MachineOriginToDownCameraX { get; set; }
        public double MachineOriginToDownCameraY { get; set; }
        public double MachineOriginToDownCameraZ { get; set; }

        public double MachineOriginToPickHeadX1 { get; set; }
        public double MachineOriginToPickHeadY1 { get; set; }
        public double MachineOriginToPickHeadZ1 { get; set; }

        public double MachineOriginToPickHeadX2 { get; set; }
        public double MachineOriginToPickHeadY2 { get; set; }
        public double MachineOriginToPickHeadZ2 { get; set; }

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

        public double DownCameraToPickHeadX { get; set; }
        public double DownCameraToPickHeadY { get; set; }

        public double DownCameraAngleX { get; set; }
        public double DownCameraAngleY { get; set; }


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
            DownCameraToPickHeadY = MachineOriginToPickHeadY1 - MachineOriginToDownCameraY + (Math.Sin(DownCameraAngleY) * targetZ);

            return (DownCameraToPickHeadX, DownCameraToPickHeadY);
        }

        public (double x_offset, double y_offset) GetItemOffsetToCamera(Point2f itemPixelLocation, double targetZ)
        /*************************************************************************************
         * When you've centered the camera but it's not quite aligned you need to call here 
         * with the approximate z of the target.  This function will return the offset.   
         ****/
        {
            DownCameraToItemX = -1 * (MachineOriginToPickHeadX1 - MachineOriginToDownCameraX + (Math.Sin(DownCameraAngleX) * targetZ));
            DownCameraToItemY = MachineOriginToPickHeadY1 - MachineOriginToDownCameraY + (Math.Sin(DownCameraAngleY) * targetZ);

            return (DownCameraToItemX, DownCameraToItemY);
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
