using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Constants
    {
        public static int CAMERA_FRAME_WIDTH = 1280;
        public static int CAMERA_FRAME_HEIGHT = 960;

        public static int SAFE_TRANSIT_Z = 20;

        public static int    DEFAULT_PART_DETECTION_THRESHOLD = 160;
        public static double CASSETTE_ORIGIN_X = -265.56;
        public static double CASSETTE_ORIGIN_Y = -118.24;

        public static double FEEDER_INITIAL_XOFFSET = 18.0;
        public static double FEEDER_ORIGIN_TO_DRIVE_YOFFSET = 42.0;
        public static double FEEDER_THICKNESS = 12.7;

        public static byte JRM_CALIBRATION_CHECK_XY = 0x99;
        public static byte JRM_CALIBRATION_CHECK_Z = 0x98;

        public static int SYSTEM_BAR_HEIGHT = 50;
        public static int HEAD_CAMERA = 0;
        public static int INSPECTION_CAMERA = 2;

        public static byte SS_MIGHTBOARD_HEADER = 0xD5;
        
        public static int BIT_PUMP = 0x20;
        public static int BIT_RELIEF = 0x40;
        public static int BIT_LIGHT = 0x10;

        public static int MAX_BUFFER_SIZE = 512;
        public static int QUEUE_SERVICE_INTERVAL = 200;

        public static int X_MAX_MILS = 11000;
        public static int Y_MAX_MILS = 7000;
        public static int Z_MAX_MILS = 6500;

        public static byte PX_AXIS = 0x80;
        public static byte PY_AXIS = 0x40;
        public static byte PA_AXIS = 0x20;
        public static byte B_AXIS = 0x10;
        public static byte A_AXIS = 0x08;
        public static byte Z_AXIS = 0x04;
        public static byte Y_AXIS = 0x02;
        public static byte X_AXIS = 0x01;

        public static byte X_AXIS_LIMIT = 0x02;
        public static byte Y_AXIS_LIMIT = 0x08;
        public static byte Z_AXIS_LIMIT = 0x10;

        public static double XY_STEPS_PER_MM = 94.139704;
        public static int XY_MAX_FEED_RATE = 18000;
        public static int XY_HOMING_FEED_RATE = 2500;
        public static double Z_STEPS_PER_MM = 400.0;
        public static int Z_MAX_FEED_RATE = 1100;
        public static int Z_HOMING_FEED_RATE = 1700;
        public static double AB_STEPS_PER_MM = 96.275201870333662468889989185642;
        public static int AB_MAX_FEED_RATE = 1600;
        public static int X_AXIS_LENGTH = 227;
        public static int Y_AXIS_LENGTH = 148;
        public static int Z_AXIS_LENGTH = 150;

        public static double CAMERA_X_OFFSET = 1211.667;

        public static byte S3G_GET_EXTENDED_POSITION_CURRENT = 21;
        public static byte S3G_GET_EXTENDED_POSITION_CURRENT_LEN = 23;

        public static double MIL_TO_MM = 0.0254;
        public static int PICK_OFFSET_MIL = 1000;
    }
}
