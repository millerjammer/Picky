using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class CalTargetModel
    {
        /* Cal Target Model Actual Dimensions */

        /* Calibration Grid Size and Location of 1st */
        public static double TARGET_GRID_X_MILS = 4000;
        public static double TARGET_GRID_Y_MILS = 4000;
        public static double TARGET_GRID_ORIGIN_X_MILS = 400;
        public static double TARGET_GRID_ORIGIN_Y_MILS = 2900;
        /* Calibration Circle at Tool Z */
        public static double TARGET_TOOL_Z_MILS = 540;
        public static double TARGET_TOOL_RADIUS_MILS = 1000;
        public static double TARGET_TOOL_POS_X_MILS = 3600;
        public static double TARGET_TOOL_POS_Y_MILS = 4900;
        /* Calibration Circle at PCB Z */
        public static double TARGET_PCB_Z_MILS = 70;
        public static double TARGET_PCB_RADIUS_MILS = 1000;
        public static double TARGET_PCB_POS_X_MILS = 1200;
        public static double TARGET_PCB_POS_Y_MILS = 4900;

    }
}
