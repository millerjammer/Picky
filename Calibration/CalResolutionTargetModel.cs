using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class CalResolutionTargetModel : INotifyPropertyChanged
    {
        private double mmPerPixX;
        public double MMPerPixX 
        { 
            get { return mmPerPixX; }
            set { mmPerPixX = value; OnPropertyChanged(nameof(MMPerPixX)); }
        }
        private double mmPerPixY;
        public double MMPerPixY
        {
            get { return mmPerPixY; }
            set { mmPerPixY = value; OnPropertyChanged(nameof(MMPerPixY)); }
        }

        private double mmHeightZ;
        public double MMHeightZ
        {
            get { return mmHeightZ; }
            set { mmHeightZ = value; OnPropertyChanged(nameof(MMHeightZ)); }
        }
        
        public CircleSegment targetCircle;  //In mm
                
        public CalResolutionTargetModel(CircleSegment tC) 
        {
            targetCircle = tC;
        }

        public bool SetMMToPixel(double radiusInPixels)
        {
            MMPerPixX = targetCircle.Radius / radiusInPixels; //  [mm/pic]
            MMPerPixY = targetCircle.Radius / radiusInPixels;
            return true;
        }

        public bool SetMMHeightZ(double currentMachineZ)
        {
            // call with machine z in mm.  This will add the current tool to give an optical distance
            MMHeightZ = currentMachineZ + Constants.TOOL_LENGTH_MM;
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
