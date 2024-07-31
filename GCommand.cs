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

        
        public static MachineMessage G_IterativeAlignToCircle(CircleSegment estimate, int max_iterations)
        {
        /*---------------------------------------------------------------
         * This function moves the head to a circle that looks like the
         * 'estimated' circle using iteration.  This is used when you don't 
         * know mm/pixel at the current Z.  This is typically used for 
         * calibration only.  Starts as a J100(position) -> J200(imaging)
         * then repeats.  After this runs the best circle will be found 
         * at GetBestCircle() in the down camera object.  The estimate is in 
         * unit of mm
         * -------------------------------------------------------------*/

            MachineMessage msg = new MachineMessage();
            msg.iterationCount = max_iterations;
            msg.cmd = Encoding.ASCII.GetBytes("J100\n");
            msg.roi = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            msg.target.x = estimate.Center.X; msg.target.y = estimate.Center.Y;
            msg.circleToFind = estimate;
                                                                
            return msg;
        }

        public static MachineMessage G_GetQRCode(int max_iterations)
        {
            MachineMessage msg = new MachineMessage();
            msg.iterationCount = max_iterations;
            msg.cmd = Encoding.ASCII.GetBytes("J101\n");
            
            return msg;
        }

        /***********************************************************
         * 
         *  CONFIGURATION AND SETTINGS COMMANDS
         *  
         ***********************************************************/


        public static MachineMessage X_SetCalFactor(char typeOfCal)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("X100 {0}\n", typeOfCal));
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
            /*--------------------------------------------------------------
             * I2C to Arduino where protocol is simple. Address is 8
             * byte index 0 is sync byte and must be '!' (33d/0x21)
             * byte index 1 is device: 0 = LED, 1 = Upper Motor
             * byte index 2 is PWM value for LED, motor on time in ms > 0 CCW
             * Important! Marlin is 0-255, the RPi is character -128 -> 127 we
             * we gotta at 128.
             * -------------------------------------------------------------*/

            MachineMessage msg = new MachineMessage();
            
            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M260 A8 B33\nM260 B0\nM260 B178 S1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M260 A8 B33\nM260 B0\nM260 B128 S1\n"));
            return msg;
        }

        public static MachineMessage G_DriveTapeAdvance(int duration)
        {
            /*---------------------------------------------------------------
             * We send a single character with range -128 - +127.  
             * Call with duration -128 to +127.  This routine will ADD 128 so
             * the range is 0-255 which the Marlin firmware will accept this 
             * 128 will be subtracted on the other side
             * --------------------------------------------------------------*/
            
            MachineMessage msg = new MachineMessage();
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M260 A8 B33\nM260 B1\nM260 B{0} S1\n", (duration + 128) ));
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
            msg.target.y = 0;
            msg.target.x = 0;
            msg.target.a = 0;
            msg.target.z = 0;
            return msg;
        }
        
       
        public static MachineMessage G_SetRPosition(double angle)
        {
        /*---------------------------------------------------------------------------
         * Servo Position
         * -------------------------------------------------------------------------*/

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
