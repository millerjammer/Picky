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
        public static byte C_ITEM_LOCATION = 0x23;

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
