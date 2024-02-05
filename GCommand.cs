/***********************************************************************
 * These are G-code commands for control of Marlin firmware
 * 
 * 
 **/



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
            msg.delay = 1;
            msg.timeout = 5000;
            return msg;  // Recommend a GetPosition
            
        }

        public static MachineMessage G_SetPositionAs(double x, double y, double z, double a, double b)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; msg.target.axis = 0x00;

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G92 X{0} Y{1} Z{2}\n", x, y, z));

            msg.delay = 1;
            msg.timeout = 5000;

            return(msg);
        }

        public static MachineMessage G_EnableSteppers(byte axis, bool enable)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            if (enable == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M17\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M18\n"));
            msg.delay = 1000;
            msg.timeout = 1000;
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

        public static MachineMessage G_SetAutoPositionReporting(bool enabled)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;
            if(enabled == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M154 S1\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("M154 S0\n"));
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage G_SetAbsolutePositioningMode(bool enabled)
        {
            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;
            if (enabled == true)
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("G90\n"));
            else
                msg.cmd = Encoding.UTF8.GetBytes(string.Format("G91\n"));
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;

        }

        public static MachineMessage G_SetPosition(double x, double y, double z, double a, double b)
        {
            /******************************************************************
            * Units are mm
            */

            MachineMessage msg = new MachineMessage();
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; 

            msg.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1} Z{2} E{3} F{4}\n", x, y, z, a, b));
            msg.delay = 1;
            msg.timeout = 5000;
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
