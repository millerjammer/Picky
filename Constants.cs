﻿using Microsoft.VisualStudio.Shell.Interop;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Picky
{
    public class Polar
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double angle { get; set; }
        public double radius { get; set; }
        public double quality { get; set; }

        public Polar(double x, double y, double z, double angle)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.angle = angle;
        }

        public Polar(double x, double y, double angle)
        {
            this.x = x;
            this.y = y;
            this.angle = angle;
        }

        
        public Polar() { }
    }
     
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
        private double z;
        public double Z
        {
            get { return z; }
            set { z = value; OnPropertyChanged(nameof(Z)); }
        }
        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            set { isValid = value; OnPropertyChanged(nameof(IsValid)); }
        }


        public Circle3d() {
            Radius = 0;
            X = Y = Z = 0;
        }
        public Circle3d(double center_x, double center_y, double z, double radius) 
        {
            Radius = radius;
            X = center_x; Y = center_y;
            Z = z;
            IsValid = false;
     
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

        /* The tools */
        public static int    TOOL_COUNT = 4;
        public static double TOOL_CENTER_RADIUS_MILS = 80;
        public static double TOOL_LENGTH_MM = 28.575;
        public static double TOOL_28GA_TIP_DIA_MM = 1.0;
        public static double TOOL_LOWER_Z_CAL = 0;
        public static double TOOL_UPPER_Z_CAL = 10;
        public static int TOOL_LOWER_THRESHOLD = 149;
        public static int TOOL_UPPER_THRESHOLD = 230;
        public static int TOOL_DETECTOR_P1 = 100;
        public static double TOOL_DETECTOR_P2 = 0.65;

        /* GUI */
        public static string PAUSE_ICON = "\uE769";
        public static string PLAY_ICON = "\uE768";

        public static int    DEFAULT_PART_DETECTION_THRESHOLD = 160;
        public static double CASSETTE_ORIGIN_X = -261.56;
        public static double CASSETTE_ORIGIN_Y = -115.24;
        
        /* Camera Messages */
        public static int FOCUS_TIP_CAL = 600;
        public static int FOCUS_TOOL_RETRIVAL = 600;
        public static int FOCUS_FEEDER_QR_CODE = 461;
        public static int FOCUS_FEEDER_PART = 481;
        public static int FOCUS_PCB_062 = 450;
        public static int FOCUS_PCB_031 = 450;

        /* Nominal Z - This is what we probe to, just enough to cause sensor to trip */
        /* These are movement distances with TOOL_LENGTH_MILS attached */
        /* If you need the optical distance to surface, add TOOL_LENGTH_MILS and CONVERSION */
        public static double FEEDER_QR_NOMINAL_Z_DRIVE_MM = 19.0;
        public static double PCB_NOMINAL_Z_DRIVE_MM = 22.0;
        public static double PART_NOMINAL_Z_DRIVE_MM = 32.0;
        public static double TOOL_NOMINAL_Z_DRIVE_MM = 40.5;

        /* Physical Constants */
        /* We use 4mm as distance from the top of the camera to the focal plane.  This is an estimate. */
        public static double CAMERA_FOCAL_PLANE_TO_CAMERA_FOCAL_PLANE_MM = 72.0715;
        public static double UP_CAMERA_FOCAL_PLANE_TO_PAD_MM = 28.5392368;
        public static double ZPROBE_LIMIT = 53.0;
        public static double ZPROBE_CAL_PAD_X = 0;
        public static double ZPROBE_CAL_PAD_Y = 185;
        public static double ZPROBE_CAL_DECK_PAD_X = 0;
        public static double ZPROBE_CAL_DECK_PAD_Y = 195;

        /* Serial Port */
        public static int MAX_BUFFER_SIZE = 4096;
        public static int QUEUE_SERVICE_INTERVAL = 100;     //100mS
        public static int OPEN_PORT_INTERVAL = 2000;        //2 Sec
        public static string SERIAL_PORT = "COM12";  
        
        /* Constants */
        public static double MIL_TO_MM = 0.0254;
        
        /* Files */
        public static string CALIBRATION_FILE_NAME = "cal.json";
        public static string BOARD_FILE_NAME = "board.json";
        public static string TOOL_FILE_NAME = "tool.json";
        public static string SETTINGS_FILE_NAME = "settings.json";
            

        public static int DOWN_CAMERA_INDEX = 0;
        public static int UP_CAMERA_INDEX = 1;
        

    }
}
