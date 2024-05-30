﻿/***********************************************************************
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
        public static MachineMessage G_SetAbsoluteXYPositionOptically(Feeder feeder)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_SET_ABSOLUTE_XY_POSITION_OPTICALLY;
            msg.feeder = feeder;
            return msg; 
        }
        
        public static MachineMessage G_SetAbsoluteXYPosition(double x, double y)
        {
            return G_SetAbsolutePosition((byte)~(Constants.X_AXIS | Constants.Y_AXIS), x, y, 0, 0, 0);
        }

        public static MachineMessage G_SetRelativeXYPosition(double x, double y)
        {
            return G_SetAbsolutePosition((byte)(0x1F), x, y, 0, 0, 0);
        }

        public static MachineMessage G_SetAbsoluteZPosition(double z)
        {
            return G_SetAbsolutePosition((byte)~(Constants.Z_AXIS), 0, 0, z, 0, 0);
        }

        public static MachineMessage G_SetRelativeZPosition(double z)
        {
            return G_SetAbsolutePosition((byte)(0x1F), 0, 0, z, 0, 0);
        }

        public static MachineMessage G_SetRelativeAPosition(double a)
        {
            return G_SetAbsolutePosition((byte)(0x1F), 0, 0, 0, a, 0);
        }

        public static MachineMessage G_SetAbsoluteAngle(double b)
        {
            return G_SetAbsolutePosition((byte)~(Constants.B_AXIS), 0, 0, 0, 0, b);
        }
        public static MachineMessage G_SetRelativeAngle(double b)
        {
            return G_SetAbsolutePosition((byte)(0x1F), 0, 0, 0, 0, b);
        }
        
        public static MachineMessage JRM_CalibrationCheckXY()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_CHECK_XY;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCheckZ()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_CHECK_Z;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCheckPick()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_CHECK_PICK;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCalculateItemResolution()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_ITEM_RESOLUTION;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCalculateItemResolution1()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_ITEM_RESOLUTION1;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCalculatePick()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_CHECK_PICK;
            return msg;
        }
        public static MachineMessage JRM_CalibrationCalculatePick1()
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_CALIBRATION_CHECK_PICK1;
            return msg;
        }

        public static MachineMessage C_LocateCircle(Rect roi)
        {
            MachineMessage msg = new MachineMessage();
            msg.roi = roi;
            msg.cmd[0] = Constants.C_ITEM_LOCATION;
            return msg;
        }

        
        public static MachineMessage X_SetCalFactor(int typeOfCal)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.X_SET_CAL_FACTOR;
            msg.calType = typeOfCal;
            return msg;
        }

       

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

        public static MachineMessage G_SetAbsolutePosition(byte axis, double x, double y, double z, double a, double b)
        {
            /******************************************************************
            * Units are mm
            * axis - bits set are relative.  If bit is clear that axis is absolute
            */

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; msg.target.axis = axis;

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1} Z{2}\n", x, y, z));
            return msg;  
            
        }

        public static MachineMessage G_SetPositionAs(double x, double y, double z, double a, double b)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; msg.target.axis = 0x00;

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
        
        public static MachineMessage G_SetZPosition(double distanceFromBed)
        {
            MachineMessage msg = new MachineMessage();
            if (distanceFromBed < 0)
                distanceFromBed = 0;
            if (distanceFromBed > Constants.Z_AXIS_MAX)
                distanceFromBed = Constants.Z_AXIS_MAX;

            // Scale - TODO convert from angular to linear
            double fraction = Constants.Z_AXIS_MAX_ROTATION / Constants.Z_AXIS_MAX;
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M280 P1 S{0}\n", Constants.Z_AXIS_MAX_ROTATION - (distanceFromBed * fraction)));
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

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1} Z{2} A{3}\n", x, y, z, a ));
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
            msg.target.axis = 0x00;

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("M114_DETAIL D\n"));
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage G_PauseResumeImmediately()
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x01;
            msg.cmd[2] = Constants.S3G_PAUSE_RESUME_IMMEDIATELY;
            msg.cmd[3] = crc8(msg.cmd, 2, 1);
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage G_AbortImmediately()
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x01;
            msg.cmd[2] = Constants.S3G_ABORT_IMMEDIATELY;
            msg.cmd[3] = crc8(msg.cmd, 2, 1);
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage G_FindZMinimum()
        {

            MachineMessage msg = new MachineMessage();
            uint rate = 400;
            uint time = 15;
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 8;
            msg.cmd[2] = 131;
            msg.cmd[3] = Constants.Z_AXIS;
            msg.cmd[4] = (byte)rate;
            msg.cmd[5] = (byte)(rate >> 8);
            msg.cmd[6] = (byte)(rate >> 16);
            msg.cmd[7] = (byte)(rate >> 24);
            msg.cmd[8] = (byte)time;
            msg.cmd[9] = (byte)(time >> 8);
            msg.cmd[10] = crc8(msg.cmd, 2, 8);
            msg.delay = 1000;
            msg.timeout = 20000;
            return msg;
        }

        public static MachineMessage G_FindXYMaximums()
        {

            MachineMessage msg = new MachineMessage();
            uint rate = 1000;
            uint time = 25;
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 8;
            msg.cmd[2] = 132;
            msg.cmd[3] = (byte)(Constants.X_AXIS | Constants.Y_AXIS);
            msg.cmd[4] = (byte)rate;
            msg.cmd[5] = (byte)(rate >> 8);
            msg.cmd[6] = (byte)(rate >> 16);
            msg.cmd[7] = (byte)(rate >> 24);
            msg.cmd[8] = (byte)time;
            msg.cmd[9] = (byte)(time >> 8);
            msg.cmd[10] = crc8(msg.cmd, 2, 8);
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static byte crc8(byte[] msg, int start_index, int len)
        /********************************************************************
         * Helper - creates CRC values.
         *
         *********************************************************************/
        {
            byte crc = 0;

            for (int i = 0; i < len; i++)
            {
                byte inbyte = msg[i + start_index];
                for (byte j = 0; j < 8; j++)
                {
                    byte mix = (byte)((crc ^ inbyte) & 0x01);
                    crc >>= 1;
                    if (mix != 0)
                        crc ^= 0x8C;

                    inbyte >>= 1;
                }
            }
            return crc;
        }
    }
}
