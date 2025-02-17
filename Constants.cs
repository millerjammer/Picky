﻿using Microsoft.VisualStudio.Shell.Interop;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Picky
{
   

    internal class Constants
    {
        /* Camera SettingsUpper - Select one */
        public static int CAMERA_FRAME_WIDTH = 2592;
        public static int CAMERA_FRAME_HEIGHT = 1944;
        public static int CAMERA_FPS = 15;
        public static int CAMERA_AUTOFOCUS = -1;
        //public static int CAMERA_FRAME_WIDTH = 1920;
        //public static int CAMERA_FRAME_HEIGHT = 1080;
        //public static int CAMERA_FPS = 30;
        //public static int CAMERA_FRAME_WIDTH = 1280;
        //public static int CAMERA_FRAME_HEIGHT = 960;
        //public static int CAMERA_FPS = 45;

        /* Camera settings */
        public static int LOWER_FOCUS = 393;
        public static int LOWER_THRESHOLD = 174;
        public static int UPPER_FOCUS = 432;
        public static int UPPER_THRESHOLD = 144;

        /* The tools */
        public static double TOOL_ORIGIN_X = 0.900;
        public static double TOOL_ORIGIN_Y = 62.80;
        public static double TOOL_ORIGIN_Y_PITCH = 25.4;
        public static double TOOL_ORIGIN_Z = 37.00;
        public static int    TOOL_DETECTOR_P1 = 100;
        public static double TOOL_DETECTOR_P2 = 0.65;
        public static int    TOOL_COUNT = 4;
        public static double TOOL_CENTER_RADIUS_MILS = 80;
        public static double TOOL_LENGTH_MM = 28.575;

        /* Feeder */
        public static double TAPE_DISTANCE_TO_TIME_MULTIPLIER = 50;

        /* GUI */
        public static string PAUSE_ICON = "\uE769";
        public static string PLAY_ICON = "\uE768";
           
        /* Camera Messages */
        public static int FOCUS_FEEDER_QR_CODE = 461;
        public static int FOCUS_FEEDER_PART = 481;
        public static int FOCUS_PCB_062 = 450;

        /* Pressure Sensor */
        public static int PRESSURE_DELTA_THRESHOLD = 8;
        public static double PRESSURE_SENSOR_RANGE_MAX = 40.0;

        /* Nominal Z - This is what we probe to, just enough to cause sensor to trip */
        /* These are movement distances with TOOL_LENGTH_MILS attached */
        /* If you need the optical distance to surface, add TOOL_LENGTH_MILS and CONVERSION */
        public static double PCB_NOMINAL_Z_DRIVE_MM = 22.0;
        public static double PART_NOMINAL_Z_DRIVE_MM = 32.0;
        public static double TOOL_NOMINAL_Z_DRIVE_MM = 40.5;

        /* Physical Constants */
        public static double ZPROBE_LIMIT = 53.0;
        public static double ZPROBE_CAL_PAD_X = 0;
        public static double ZPROBE_CAL_PAD_Y = 170;
        public static double ZPROBE_CAL_DECK_PAD_X = 0;
        public static double ZPROBE_CAL_DECK_PAD_Y = 188;
        public static double ZPROBE_STEP_PAD_X = 195;
        public static double ZPROBE_STEP_PAD_Y = 0;

        public static double TRAVEL_LIMIT_X_MM = 284.00;
        public static double TRAVEL_LIMIT_Y_MM = 245.00;

        public static double QR_CODE_SIZE_MM = 12.0;

        public static double CAMERA_TO_HEAD_OFFSET_Y_MM = 17;
        public static double CAMERA_TO_HEAD_OFFSET_X_MM = 1;

        public static double DEFAULT_SKEW_X = 0;

        public static double DEFAULT_MACHINE_DRIVELINE = 248.3;
        
        public static double DEFAULT_MM_PER_PIXEL = .0206;    //Assumes a FOV

        /* Physical constants beyond where we can probe */
        public static double ZOFFSET_CAL_PAD_TO_FEEDER_TAPE = 11.4;  //Temp!!!! TODO replace with measurement and focus and all that
        public static double ZOFFSET_CAL_PAD_TO_DECK = 8.0;
        public static double ZOFFSET_CAL_PAD_TO_QR = 7.0;

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

        public static string FEEDER_TEMPLATE_FILE_EXTENTION = ".png";
        public static string FEEDER_FILE_EXTENTION = ".fdr";
        public static string CASSETTE_FILE_EXTENTION = ".cst";


        public static int DOWN_CAMERA_INDEX = 0;
        public static int UP_CAMERA_INDEX = 1;

        


    }
}
