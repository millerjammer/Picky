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
        
        /* For 1280x960 */
        //public static int CALIBRATION_TARGET_WIDTH_DEFAULT_PIX = 950;
        //public static int CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX = 609;
        /* For 2592x1944 */
        public static int CALIBRATION_TARGET_WIDTH_DEFAULT_PIX = 1987;
        public static int CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX = 1275;

        /* Dimensions of the actual calibration target */
        public static double CALIBRATION_TARGET_WIDTH_MM = 69.85;
        public static double CALIBRATION_TARGET_HEIGHT_MM = 44.45;
        public static double CALIBRATION_TARGET_DIST_MM = 25.0;

        public static double CALIBRATION_TARGET_LOCATION_X_MM = -72.0;
        public static double CALIBRATION_TARGET_LOCATION_Y_MM = -55.0;

        /* FIxed focus and height for pick calibration offset */
        public static double CALIBRATION_PICK_HEIGHT_MM = 45.0;
        public static int CALIBRATION_PICK_FOCUS = 390;

        /* Due to Barrel distortion and angular alignment */
        public static double PICK_DISTORTION_OFFSET_X_MM = 0;//0.15;
        public static double PICK_DISTORTION_OFFSET_Y_MM = 0;//0.20;
        public static double PLACE_DISTORTION_OFFSET_X_MM = 0.0;
        public static double PLACE_DISTORTION_OFFSET_Y_MM = 0.0;


        public static int SAFE_TRANSIT_Z = 45;

        public static int    DEFAULT_PART_DETECTION_THRESHOLD = 160;
        public static double CASSETTE_ORIGIN_X = -261.56;
        public static double CASSETTE_ORIGIN_Y = -115.24;

        public static double PART_TO_PICKUP_XOFFSET_MM = 0;
        public static double PART_TO_PICKUP_YOFFSET_MM = 0;
        public static double PART_TO_PICKUP_Z = 18.2;
        

        public static double CASSETTE_TO_INITIAL_FEEDER_XOFFSET = 18.0;
        public static double CASSETTE_TO_INITIAL_FEEDER_YOFFSET = -2.0;
        public static double FEEDER_TO_PLATFORM_ZOFFSET = -19.0;
        public static double FEEDER_ORIGIN_TO_PART_TRAY_START = -5.00;
        public static double FEEDER_ORIGIN_TO_PART_TRAY_END = -17.00;
        public static double FEEDER_ORIGIN_TO_DRIVE_YOFFSET = -40.06;
        public static double FEEDER_ORIGIN_TO_DRIVE_XOFFSET = -2.10;
        public static double FEEDER_DRIVE_ABSOLUTE_Z = 35.0;
        public static double FEEDER_THICKNESS = 12.7;

        public static double PCB_MAX_DIMENSION_X = 245;
        public static double PCB_MAX_DIMENSION_Y = 100;

        /* Special Messages */
        public static byte JRM_CALIBRATION_CHECK_XY = 0x99;
        public static byte JRM_CALIBRATION_CHECK_Z = 0x98;
        public static byte JRM_CALIBRATION_ITEM_RESOLUTION = 0x97;
        public static byte JRM_CALIBRATION_ITEM_RESOLUTION1 = 0x96;
        public static byte JRM_CALIBRATION_CHECK_PICK = 0x95;
        public static byte JRM_CALIBRATION_CHECK_PICK1 = 0x94;
        public static byte JRM_SET_PICKLIST_INDEX = 0x93;

        public static byte JRM_SET_ABSOLUTE_XY_POSITION_OPTICALLY = 0x92;

        public static byte SS_MIGHTBOARD_HEADER = 0xD5;
        
        public static int BIT_PUMP = 0x20;
        public static int BIT_RELIEF = 0x40;
        public static int BIT_LIGHT = 0x10;

        public static int MAX_BUFFER_SIZE = 512;
        public static int QUEUE_SERVICE_INTERVAL = 200;
            

        public static byte PX_AXIS = 0x80;
        public static byte PY_AXIS = 0x40;
        public static byte PA_AXIS = 0x20;
        public static byte B_AXIS = 0x10;
        public static byte A_AXIS = 0x08;
        public static byte Z_AXIS = 0x04;
        public static byte Y_AXIS = 0x02;
        public static byte X_AXIS = 0x01;

        public static byte X_AXIS_MIN_SW = 0x01;
        public static byte X_AXIS_MAX_SW = 0x02;
        public static byte Y_AXIS_MIN_SW = 0x04;
        public static byte Y_AXIS_MAX_SW = 0x08;
        public static byte Z_AXIS_MIN_SW = 0x10;
        public static byte Z_AXIS_MAX_SW = 0x20;

        public static double XY_STEPS_PER_MM = 94.139704;
        public static int XY_MAX_FEED_RATE = 18000;
        public static int XY_HOMING_FEED_RATE = 2500;
        public static double Z_STEPS_PER_MM = 400.0;
        public static int Z_MAX_FEED_RATE = 1100;
        public static int Z_HOMING_FEED_RATE = 1700;
        public static double AB_STEPS_PER_MM = 96.275201870333662468889989185642;
        public static double B_DEGREES_PER_MM = 3.60;
        public static int AB_MAX_FEED_RATE = 1600;
        public static int X_AXIS_LENGTH = 227;
        public static int Y_AXIS_LENGTH = 148;
        public static int Z_AXIS_LENGTH = 150;

        /* Exceeding these will result in position error and release of all steppers */
        public static double LIMIT_ABSOLUTE_Z = 80;
        public static double LIMIT_ABSOLUTE_X = -270;
        public static double LIMIT_ABSOLUTE_Y = -160;

        /* Commands */
        public static byte S3G_GET_EXTENDED_POSITION_CURRENT = 21;
        public static byte S3G_GET_EXTENDED_POSITION_CURRENT_LEN = 23;
        public static byte S3G_ABORT_IMMEDIATELY = 7;
        public static byte S3G_PAUSE_RESUME_IMMEDIATELY = 8;

        public static double MIL_TO_MM = 0.0254;
        public static int PICK_OFFSET_MIL = 1000;

        public static string CALIBRATION_FILE_NAME = "cal.json";
    
    }
}
