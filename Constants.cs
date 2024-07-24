using Microsoft.VisualStudio.Shell.Interop;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Picky
{
    public class Circle3d : INotifyPropertyChanged
    {
        private double radius;
        public double Radius
        {
             get { return radius; }
             set { radius = value; OnPropertyChanged(nameof(Radius)); }
        }
        private double x;
        public double X
        {
            get { return x; }
            set { x = value; OnPropertyChanged(nameof(X)); }
        }
        private double y;
        public double Y
        {
            get { return y; }
            set { y = value; OnPropertyChanged(nameof(Y)); }
        }
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; OnPropertyChanged(nameof(IsValid)); }
        }


        public Circle3d() {
            Radius = 0;
            X = Y = 0;
        }
        public Circle3d(double center_x, double center_y, double radius) 
        {
            Radius = radius;
            X = center_x; Y = center_y;
            IsValid = false;
     
        }
        
        public override string  ToString()
        {
            return string.Format("Circle3d: Point2d.X: {0} X: {1} Y: {2} IsValid: {3}", Radius, X, Y, IsValid);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class Constants
    {
        
        /* Camera Settings - Select one */
        public static int CAMERA_FRAME_WIDTH = 2592;
        public static int CAMERA_FRAME_HEIGHT = 1944;
        public static int CAMERA_FPS = 15;
        //public static int CAMERA_FRAME_WIDTH = 1920;
        //public static int CAMERA_FRAME_HEIGHT = 1080;
        //public static int CAMERA_FPS = 30;
        //public static int CAMERA_FRAME_WIDTH = 1280;
        //public static int CAMERA_FRAME_HEIGHT = 960;
        //public static int CAMERA_FPS = 45;

        public static double CAMERA_OFFSET_Z = 50.8;

        /* Using 3.29mm focal length lens and 3.6736mm x 2.7384mm sensor */
        /* In mm */
        
        public static int TOOL_COUNT = 4;
        public static double TOOL_CENTER_RADIUS_MILS = 80;

        public static int    DEFAULT_PART_DETECTION_THRESHOLD = 160;
        public static double CASSETTE_ORIGIN_X = -261.56;
        public static double CASSETTE_ORIGIN_Y = -115.24;

        public static double PART_TO_PICKUP_XOFFSET_MM = 0;
        public static double PART_TO_PICKUP_YOFFSET_MM = 0;
        public static double PART_TO_PICKUP_Z = 18.2;
        
        /* Special Messages */

        /* Calibration and Settings Messages */
        public static byte X_SET_CAL_FACTOR = 0x22;

        /* Camera Messages */
        public static byte C_ALIGN_TO_CIRCLE = 0x21;
        public static byte C_ALIGN_TO_CIRCLE_IMAGE = 0x24;

        /* Calibration sub-type Messages */
        public static byte CAL_TYPE_RESOLUTION_AT_PCB = 0x10;
        public static byte CAL_TYPE_Z_DISTANCE_AT_PCB = 0x11;
        public static byte CAL_TYPE_RESOLUTION_AT_TOOL = 0x12;
        public static byte CAL_TYPE_Z_DISTANCE_AT_TOOL = 0x13;

        public static byte JRM_SET_ABSOLUTE_XY_POSITION_OPTICALLY = 0x92;

        /* Serial Port */       
        public static int MAX_BUFFER_SIZE = 4096;
        public static int QUEUE_SERVICE_INTERVAL = 100;
            
        /* Constants */
        public static double MIL_TO_MM = 0.0254;
        
        /* Files */
        public static string CALIBRATION_FILE_NAME = "cal.json";
        public static string TOOL_FILE_NAME = "tool.json";

    }
}
