using Microsoft.VisualStudio.OLE.Interop;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace Picky
{
    public class CalibrationModel : INotifyPropertyChanged
    {

        /* Calibration Parameters - Pick Tool */
        private PickToolModel pickToolCal;
        public PickToolModel PickToolCal
        {
            get { return pickToolCal; }
            set { pickToolCal = value; }
        }

        /* Calibration Parameters - mm/steps*/
        private OpenCvSharp.Rect refObject;
        public OpenCvSharp.Rect RefObject
        {
            get { return refObject; }
            set { Console.WriteLine("Reference Updated. Old: " + refObject.Width + " " + refObject.Height + " new: " + value.Width + " " + value.Height); refObject = value;  }
        }

        /* Resolution */
        private double pcbMMToPixX;
        public double PcbMMToPixX { get { return pcbMMToPixX; } set { pcbMMToPixX = value; OnPropertyChanged(nameof(PcbMMToPixX)); } }
        private double pcbMMToPixY;
        public double PcbMMToPixY { get { return pcbMMToPixY; } set { pcbMMToPixY = value; OnPropertyChanged(nameof(PcbMMToPixY)); } }
        private double pcbZHeight;
        public double PcbZHeight { get { return pcbZHeight; } set { pcbZHeight = value; OnPropertyChanged(nameof(PcbZHeight)); } }

        private double toolMMToPixX;
        public double ToolMMToPixX { get { return toolMMToPixY; } set { toolMMToPixX = value; OnPropertyChanged(nameof(ToolMMToPixX)); } }
        private double toolMMToPixY;
        public double ToolMMToPixY { get { return toolMMToPixY; } set { toolMMToPixY = value; OnPropertyChanged(nameof(ToolMMToPixY)); } }
        private double toolZHeight;
        public double ToolZHeight { get { return pcbZHeight; } set { pcbZHeight = value; OnPropertyChanged(nameof(ToolZHeight)); } }

        private double feederMMToPixX;
        public double FeederMMToPixX { get { return feederMMToPixY; } set { feederMMToPixX = value; OnPropertyChanged(nameof(FeederMMToPixX)); } }
        private double feederMMToPixY;
        public double FeederMMToPixY { get { return feederMMToPixY; } set { feederMMToPixY = value; OnPropertyChanged(nameof(FeederMMToPixY)); } }
        private double feederZHeight;
        public double FeederZHeight { get { return pcbZHeight; } set { pcbZHeight = value; OnPropertyChanged(nameof(FeederZHeight)); } }

        /* Camera/Pick Physics */
        private double machineOriginToDownCameraX;
        public double MachineOriginToDownCameraX { get { return machineOriginToDownCameraX; } set { machineOriginToDownCameraX = value; } }
        private double machineOriginToDownCameraY;
        public double MachineOriginToDownCameraY { get { return machineOriginToDownCameraY; } set { machineOriginToDownCameraY = value; } }
        private double machineOriginToDownCameraZ;
        public double MachineOriginToDownCameraZ { get { return machineOriginToDownCameraZ; } set { machineOriginToDownCameraZ = value; } }

        private double machineOriginToPickHeadX1;
        public double MachineOriginToPickHeadX1 { get { return machineOriginToPickHeadX1; } set { machineOriginToPickHeadX1 = value; calcDownAngles(); } }
        private double machineOriginToPickHeadY1;
        public double MachineOriginToPickHeadY1 { get { return machineOriginToPickHeadY1; } set { machineOriginToPickHeadY1 = value; calcDownAngles(); } }
        private double machineOriginToPickHeadZ1;
        public double MachineOriginToPickHeadZ1 { get { return machineOriginToPickHeadZ1; } set { machineOriginToPickHeadZ1 = value; calcDownAngles(); } }

        private double machineOriginToPickHeadX2;
        public double MachineOriginToPickHeadX2 { get { return machineOriginToPickHeadX2; } set { machineOriginToPickHeadX2 = value; calcDownAngles(); } }
        private double machineOriginToPickHeadY2;
        public double MachineOriginToPickHeadY2 { get { return machineOriginToPickHeadY2; } set { machineOriginToPickHeadY2 = value; calcDownAngles(); } }
        private double machineOriginToPickHeadZ2;
        public double MachineOriginToPickHeadZ2 { get { return machineOriginToPickHeadZ2; } set { machineOriginToPickHeadZ2 = value; calcDownAngles(); } }

        /*Calculated */
        public double DownCameraToItemX { get; set; }
        public double DownCameraToItemY { get; set; }

        public double DownCameraToPickHeadX { get; set; }
        public double DownCameraToPickHeadY { get; set; }

        public double DownCameraAngleX { get; set; }
        public double DownCameraAngleY { get; set; }


        public CalibrationModel() 
        {
            pickToolCal = new PickToolModel();
            refObject = new OpenCvSharp.Rect(0, 0, Constants.CALIBRATION_TARGET_WIDTH_DEFAULT_PIX, Constants.CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX);
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
            DownCameraToPickHeadX = -1 * ( MachineOriginToPickHeadX1 - MachineOriginToDownCameraX + (Math.Sin(DownCameraAngleX) * targetZ));
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
         * Update calculation angles. 
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
