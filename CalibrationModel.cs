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

        /*Calculated */
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
            double dist = MachineOriginToPickHeadZ1 - MachineOriginToPickHeadZ2;
            if (dist == 0)
            {
                Console.WriteLine("Calibration Error: Z axis distances is zero");
                return (0, 0);
            }
            DownCameraAngleX = Math.Atan((MachineOriginToPickHeadX1 - MachineOriginToPickHeadX2) / (dist));
            DownCameraAngleY = Math.Atan((MachineOriginToPickHeadY1 - MachineOriginToPickHeadY2) / (dist));

            DownCameraToPickHeadX = MachineOriginToPickHeadX1 - MachineOriginToDownCameraX + (Math.Sin(DownCameraAngleX) * targetZ);
            DownCameraToPickHeadY = MachineOriginToPickHeadY1 - MachineOriginToDownCameraY + (Math.Sin(DownCameraAngleY) * targetZ);

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
