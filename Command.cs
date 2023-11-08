

/*  For S3G Serial Commands See: https://github.com/makerbot/s3g/blob/master/doc/s3gProtocol.md */
namespace Picky
{
    public class Command
    {
        public static MachineMessage S3G_SetAbsoluteXYPositionOptically(Feeder feeder)
        {
            MachineMessage msg = new MachineMessage();
            msg.cmd[0] = Constants.JRM_SET_ABSOLUTE_XY_POSITION_OPTICALLY;
            msg.feeder = feeder;
            return msg; 
        }
        
        public static MachineMessage S3G_SetAbsoluteXYPosition(double x, double y)
        {
            return S3G_SetAbsolutePosition((byte)~(Constants.X_AXIS | Constants.Y_AXIS), x, y, 0, 0, 0);
        }

        public static MachineMessage S3G_SetRelativeXYPosition(double x, double y)
        {
            return S3G_SetAbsolutePosition((byte)(0x1F), x, y, 0, 0, 0);
        }

        public static MachineMessage S3G_SetAbsoluteZPosition(double z)
        {
            return S3G_SetAbsolutePosition((byte)~(Constants.Z_AXIS), 0, 0, z, 0, 0);
        }

        public static MachineMessage S3G_SetRelativeZPosition(double z)
        {
            return S3G_SetAbsolutePosition((byte)(0x1F), 0, 0, z, 0, 0);
        }

        public static MachineMessage S3G_SetRelativeAPosition(double a)
        {
            return S3G_SetAbsolutePosition((byte)(0x1F), 0, 0, 0, a, 0);
        }

        public static MachineMessage S3G_SetAbsoluteAngle(double b)
        {
            return S3G_SetAbsolutePosition((byte)~(Constants.B_AXIS), 0, 0, 0, 0, b);
        }
        public static MachineMessage S3G_SetRelativeAngle(double b)
        {
            return S3G_SetAbsolutePosition((byte)(0x1F), 0, 0, 0, 0, b);
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


        public static MachineMessage S3G_SetAbsolutePosition(byte axis, double x, double y, double z, double a, double b)
        {
            /******************************************************************
            * Units are mm
            * axis - bits set are relative.  If bit is clear that axis is absolute
            */

            MachineMessage msg = new MachineMessage();
            uint xx, yy, zz, aa, bb, duration;
            
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; msg.target.axis = axis;
            

            xx = (uint)(x * Constants.XY_STEPS_PER_MM);
            yy = (uint)(y * Constants.XY_STEPS_PER_MM);
            zz = (uint)(z * Constants.Z_STEPS_PER_MM);
            aa = (uint)(a * Constants.AB_STEPS_PER_MM);
            bb = (uint)(b * Constants.AB_STEPS_PER_MM);
            duration = 2 * 1000000;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 26;
            msg.cmd[2] = 142;
            //X absolute position
            msg.cmd[3] = (byte)xx; msg.cmd[4] = (byte)(xx >> 8); msg.cmd[5] = (byte)(xx >> 16); msg.cmd[6] = (byte)(xx >> 24);
            //Y absolute position
            msg.cmd[7] = (byte)yy; msg.cmd[8] = (byte)(yy >> 8); msg.cmd[9] = (byte)(yy >> 16); msg.cmd[10] = (byte)(yy >> 24);
            //Z absolute position
            msg.cmd[11] = (byte)zz; msg.cmd[12] = (byte)(zz >> 8); msg.cmd[13] = (byte)(zz >> 16); msg.cmd[14] = (byte)(zz >> 24);
            //A absolute position
            msg.cmd[15] = (byte)aa; msg.cmd[16] = (byte)(aa >> 8); msg.cmd[17] = (byte)(aa >> 16); msg.cmd[18] = (byte)(aa >> 24);
            //B absolute position
            msg.cmd[19] = (byte)bb; msg.cmd[20] = (byte)(bb >> 8); msg.cmd[21] = (byte)(bb >> 16); msg.cmd[22] = (byte)(bb >> 24);
            //Set duration
            msg.cmd[23] = (byte)duration; msg.cmd[24] = (byte)(duration >> 8); msg.cmd[25] = (byte)(duration >> 16); msg.cmd[26] = (byte)(duration >> 24);
            // Set all moves absolute
            msg.cmd[27] = axis; //SET = Relative movement CLR = Absolute
            msg.cmd[28] = crc8(msg.cmd, 2, 26);
            msg.delay = 1;
            msg.timeout = 5000;
            return msg;  // Recommend a GetPosition
            
        }

        public static MachineMessage S3G_SetPositionAs(double x, double y, double z, double a, double b)
        {

            MachineMessage msg = new MachineMessage();
            uint xx, yy, zz, aa, bb;
            
            msg.target.x = x; msg.target.y = y;
            msg.target.z = z; msg.target.a = a;
            msg.target.b = b; msg.target.axis = 0x00;

            xx = (uint)(x * Constants.XY_STEPS_PER_MM);
            yy = (uint)(y * Constants.XY_STEPS_PER_MM);
            zz = (uint)(z * Constants.Z_STEPS_PER_MM);
            aa = (uint)(a * Constants.AB_STEPS_PER_MM);
            bb = (uint)(b * Constants.AB_STEPS_PER_MM);

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 21;
            msg.cmd[2] = 140;
            //X absolute position
            msg.cmd[3] = (byte)xx; msg.cmd[4] = (byte)(xx >> 8); msg.cmd[5] = (byte)(xx >> 16); msg.cmd[6] = (byte)(xx >> 24);
            //Y absolute position
            msg.cmd[7] = (byte)yy; msg.cmd[8] = (byte)(yy >> 8); msg.cmd[9] = (byte)(yy >> 16); msg.cmd[10] = (byte)(yy >> 24);
            //Z absolute position
            msg.cmd[11] = (byte)zz; msg.cmd[12] = (byte)(zz >> 8); msg.cmd[13] = (byte)(zz >> 16); msg.cmd[14] = (byte)(zz >> 24);
            //A absolute position
            msg.cmd[15] = (byte)aa; msg.cmd[16] = (byte)(aa >> 8); msg.cmd[17] = (byte)(aa >> 16); msg.cmd[18] = (byte)(aa >> 24);
            //B absolute position
            msg.cmd[19] = (byte)bb; msg.cmd[20] = (byte)(bb >> 8); msg.cmd[21] = (byte)(bb >> 16); msg.cmd[22] = (byte)(bb >> 24);
            msg.cmd[23] = crc8(msg.cmd, 2, 21);
            msg.delay = 1;
            msg.timeout = 5000;

            return(msg);
        }

        public static MachineMessage S3G_EnableSteppers(byte axis, bool enable)
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            if (enable == true)
                axis |= 0x80;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x02;
            msg.cmd[2] = 137;
            msg.cmd[3] = axis;
            msg.cmd[4] = crc8(msg.cmd, 2, 2);
            msg.delay = 1000;
            msg.timeout = 1000;
            return msg;
        }

        public static MachineMessage S3G_BusyCheck()
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x01;
            msg.cmd[2] = 11;
            msg.cmd[3] = crc8(msg.cmd, 2, 1);
            msg.delay = 1000;
            msg.timeout = 1000;
            return msg;
        }

        public static MachineMessage S3G_Initialize()
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x01;
            msg.cmd[2] = 0x01;
            msg.cmd[3] = crc8(msg.cmd, 2, 1);
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage S3G_PauseResumeImmediately()
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

        public static MachineMessage S3G_AbortImmediately()
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


        public static MachineMessage S3G_GetPosition()
        {

            MachineMessage msg = new MachineMessage();
            msg.target.axis = 0x00;

            msg.cmd[0] = Constants.SS_MIGHTBOARD_HEADER;
            msg.cmd[1] = 0x01;
            msg.cmd[2] = Constants.S3G_GET_EXTENDED_POSITION_CURRENT;
            msg.cmd[3] = crc8(msg.cmd, 2, 1);
            msg.delay = 1000;
            msg.timeout = 5000;
            return msg;
        }

        public static MachineMessage S3G_FindZMinimum()
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

        public static MachineMessage S3G_FindXYMaximums()
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
