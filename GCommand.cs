/***********************************************************************
 * These are G-code commands for control of Marlin firmware
 * 
 * 
 **/



using Microsoft.VisualStudio.Shell.Interop;
using OpenCvSharp;
using System;
using System.Text;
using System.Windows.Forms.VisualStyles;

namespace Picky
{
    public class GCommand
    {
        /***********************************************************
         * 
         *  CAMERA AND IMAGING COMMANDS
         *  
         ***********************************************************/

        public static MachineMessage C_LocateCircle(Rect roi)
        {
            MachineMessage msg = new MachineMessage();
            msg.roi = roi;
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("L0 CIR X{0} Y{1} W{2} H{3}\n", roi.X, roi.Y, roi.Width, roi.Height));
            return msg;
        }

        public static MachineMessage G_IterativeAlignToCircle(CircleSegment estimate, Circle3d result, int max_iterations)
        {
        /*---------------------------------------------------------------
         * This function moves the head to a circle that looks like the
         * 'estimated' circle using iteration.  This is used when you don't 
         * know mm/pixel at the current Z.  This is typically used for 
         * calibration only.  Starts as a J100(position) -> J200(imaging)
         * then repeats.  After this runs the best circle will be found 
         * at GetBestCircle() in the down camera object.
         * -------------------------------------------------------------*/

            MachineMessage msg = new MachineMessage();
            msg.iterationCount = max_iterations;
            msg.cmd = Encoding.ASCII.GetBytes("J100\n");
            msg.roi = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            msg.target.x = estimate.Center.X; msg.target.y = estimate.Center.Y;
            msg.circleToFind = estimate;
                                                                
            return msg;
        }

        /***********************************************************
         * 
         *  CONFIGURATION AND SETTINGS COMMANDS
         *  
         ***********************************************************/


        public static MachineMessage X_SetCalFactor(int typeOfCal)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.X_SET_CAL_FACTOR;
            msg.calType = typeOfCal;
            return msg;
        }

        /***********************************************************
        * 
        *  MOTION CONTROL COMMANDS
        *  
        ***********************************************************/


        public static MachineMessage G_ProbeZ(double z)
        {
            /******************************************************************
            * Units are mm, z is absolute
            * See https://marlinfw.org/docs/gcode/G038.html
            */

            MachineMessage msg = new MachineMessage();
            msg.target.z = z;

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G38.5 Z{0}\n", z));
            return msg;

        }
               
        public static MachineMessage G_SetPosition(double x, double y, double z, double a, double b)
        {
            /******************************************************************
            * Units are mm
            * TODO b is actuall F{4} feedrate
            */

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b;

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1} Z{2} A{3}\n", x, y, z, a));
            return msg;
        }

        public static MachineMessage G_SetPositionAs(double x, double y, double z, double a, double b)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; 

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G92 X{0} Y{1} Z{2}\n", x, y, z));
            return(msg);
        }

        public static MachineMessage G_EnableSteppers(byte axis, bool enable)
        {
            MachineMessage msg = new MachineMessage();
            
            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M17\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M18\n"));
            return msg;
        }

        public static MachineMessage G_EnableIlluminator(bool enable)
        {
            MachineMessage msg = new MachineMessage();
            
            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M260 A8 B33\nM260 B0\nM260 B50 S1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M260 A8 B33\nM260 B0\nM260 B0 S1\n"));
            return msg;
        }

        public static MachineMessage G_EnableUpIlluminator(bool enable)
        {
            MachineMessage msg = new MachineMessage();

            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P204 S255 T1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P204 S0 T1\n"));
            return msg;
        }

        public static MachineMessage G_EnablePump(bool enable)
        {
            MachineMessage msg = new MachineMessage();

            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P207 S255 T1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P207 S0 T1\n"));
            return msg;
        }

        public static MachineMessage G_EnableValve(bool enable)
        {
            MachineMessage msg = new MachineMessage();

            //Uses FAN0 Connection - M42 command won't work? TODO
            if (enable == true)
                //msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P203 S255 T1\n"));
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M106 P0 S255\n"));
            else
                //msg.cmd = Encoding.UTF8.GetBytes(string.Format("M42 I1 P203 S0 T1\n"));
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M106 P0 S0\n"));
            return msg;
        }

        public static MachineMessage G_OpenToolStorage(bool open)
        {
            MachineMessage msg = new MachineMessage();

            if (open == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M280 P0 S50\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M280 P0 S9\n"));
            return msg;
        }

        public static MachineMessage G_GetStepsPerUnit()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M92\n"));
            return msg;
        }

        public static MachineMessage G_SetStepsPerUnit(double x_steps_per_unit, double y_steps_per_unit)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M92 X{0} Y{1}\n", x_steps_per_unit, y_steps_per_unit));
            return msg;
        }
               

        public static MachineMessage G_SetAutoPositionReporting(bool enabled)
        {

            MachineMessage msg = new MachineMessage();
            if(enabled == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M154 S1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M154 S0\n"));
            return msg;
        }

        public static MachineMessage G_FindMachineHome()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G28 Z\nG28 A\nG28 Y\nG28 X\n"));
            return msg;
        }
        
       
        public static MachineMessage G_SetRPosition(double angle)
        {
            MachineMessage msg = new MachineMessage();
            if (angle < 0)
                angle = 0;
            if (angle > 360)
                angle = (angle % 360) * 360;
            if (angle > 180)
                angle -= 180;
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M280 P2 S{0}\n", angle));
            return msg;
        }

        public static MachineMessage G_SetAbsolutePositioningMode(bool enabled)
        {
            MachineMessage msg = new MachineMessage();
            msg.delay = 0;
            if (enabled == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("G90\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("G91\n"));
            return msg;

        }

        public static MachineMessage G_FinishMoves()
        {
            MachineMessage msg = new MachineMessage();
            msg.delay = 0;
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M400\n"));
            return msg;
        }

        public static MachineMessage G_SetItemLocation()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G0 *\n"));
            return msg;
        }

        public static MachineMessage G_GetPosition()
        {

            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M114_DETAIL D\n"));
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

      

       

        
    }
}
