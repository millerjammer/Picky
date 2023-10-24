using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Picky
{
    public class SerialInterface
    {
        private static byte[] serial_buffer = new byte[Constants.MAX_BUFFER_SIZE];
        private static int s_in = 0;

        MachineModel machine = MachineModel.Instance;

        private static MachineMessage.Pos lastPos = new MachineMessage.Pos();
        public int rx_msgCount { get; set; }
        public int tx_msgCount { get; set; }
        
        static SerialPort serialPort;

        public SerialInterface() {

            /* Open Port */
            serialPort = new SerialPort();
            serialPort.PortName = "COM4";
            serialPort.BaudRate = 115200;
            try
            {
                serialPort.Open();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Couldn't Open Port:" + exc.Message);
            }
            if (serialPort.IsOpen)
            {
                Console.WriteLine("Machine Serial Port Opened.");
            }
                     
            /* Start a Timer to Handle Message Queue */
            System.Timers.Timer msgTimer = new System.Timers.Timer();
            msgTimer.Elapsed += new ElapsedEventHandler(OnTimer);
            msgTimer.Interval = Constants.QUEUE_SERVICE_INTERVAL;
            msgTimer.Enabled = true;
        }

        public void OnTimer(object source, ElapsedEventArgs e)
        /************************************************************
         * Yes, it's all handled on a timer rather than when data comes
         * in.  This reduces overhead since it's more likely there's a 
         * complete message in the queue to process. 
         * **********************************************************/
        {
            if (ServicePickPort() == true)
            {
                ServiceMessageQueue();
            }
        }

        private bool ServicePickPort()
        /********************************************************************
         * Checks and processes serial data from the machine.
         * Returns true if a message was successfully parsed.
         *********************************************************************/
        {

            int length, i, j, k;
            int bytes_read;
            byte[] sbuf = new byte[64];
            string rawString = " ";
            bool isPositionGood = true;

            if (serialPort.IsOpen)
            {
                /**** Read the port, stick it at end of the main buffer *****/
                if (serialPort.BytesToRead == 0)
                {
                    return true;
                }
                bytes_read = serialPort.Read(sbuf, 0, 64);
                for (i = 0; i < bytes_read; i++)
                {
                    serial_buffer[s_in++] = sbuf[i];
                }
                //Try to read a message 
                for (i = 0; i < s_in; i++)
                {
                    if (serial_buffer[i] == (byte)Constants.SS_MIGHTBOARD_HEADER)
                    {
                        length = serial_buffer[i + 1] + 3;
                        if (length <= (s_in - i))
                        {
                            //There are enough bytes to parse a message
                            for (j = 0; j < length; j++)
                            {
                                //Read the Message and put in Log
                                rawString += " " + String.Format("{0:X}", serial_buffer[i + j] & 0xFF);
                            }
                            //dlg.m_messageBox.InsertString(-1, str);
                            rx_msgCount++;
                            //Check return message
                            if (serial_buffer[i + 1] == (byte)Constants.S3G_GET_EXTENDED_POSITION_CURRENT_LEN)
                            {
                                isPositionGood = getPositionFromMessage();
                            }
                            //Fixup the byte buffer by moving unread bytes and the pointer
                            for (k = 0; k < Constants.MAX_BUFFER_SIZE - length; k++)
                            {
                                serial_buffer[k] = serial_buffer[i + length];
                            }
                            s_in -= i + length;
                            if (isPositionGood == true && machine.Messages.Count > 0)
                            {
                                Console.WriteLine("RX Count: " + rx_msgCount.ToString());
                                Console.WriteLine("RX Data: " + rawString);
                                machine.Messages.RemoveAt(0);
                            }
                            return true;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private bool ServiceMessageQueue()
        /********************************************************************
        * This is outbound messages to the machine
        *
        *********************************************************************/
        {
            if (machine.Messages.Count() > 0)
            {
                MachineMessage msg = machine.Messages.First();
                if (msg.cmd[0] == Constants.JRM_CALIBRATION_CHECK_XY)
                {
                    Console.WriteLine("Calibration XY Check: " + machine.LastEndStopState);
                    if((machine.LastEndStopState & (Constants.X_AXIS_LIMIT & Constants.Y_AXIS_LIMIT)) == (Constants.X_AXIS_LIMIT & Constants.Y_AXIS_LIMIT))
                    {
                        machine.CalibrationStatusString = "Calibration XY: Successful";
                        machine.Messages.RemoveAt(0);
                    }
                    else
                    {
                        machine.CalibrationStatusString = "Calibration XY: Failed (Timeout?)";
                        machine.Messages.Clear();
                    }
                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_CHECK_Z)
                {
                    Console.WriteLine("Calibration Z Check: " + machine.LastEndStopState);
                    if ((machine.LastEndStopState & (Constants.Z_AXIS_LIMIT)) == (Constants.Z_AXIS_LIMIT))
                    {
                        machine.CalibrationStatusString = "Calibration Z: Successful";
                        machine.Messages.RemoveAt(0);
                    }
                    else
                    {
                        machine.CalibrationStatusString = "Calibration Z: Failed (Timeout?)";
                        machine.Messages.Clear();
                    }
                }
                else
                {
                    serialPort.Write(msg.cmd, 0, msg.cmd[1] + 3);
                    if (msg.target.axis != 0)
                    {
                        //dlg.SetActivePickLocation(messages.front().target);
                    }
                    tx_msgCount++;
                }
            }
            return true;
        }

        private bool getPositionFromMessage()
        /********************************************************************
         * Helper - Updates current position (from message 21)
         *
         *********************************************************************/
        {
            /*
            * Return true if position is good (close to target position)
            */
            bool isGood = false;
            if (machine.Messages.Count == 0)
            {
                //If the queue is empty
                //dlg.m_messageBox.AddString(L"Get Position Request: Queue Empty.");
                return true;
            }
            MachineMessage.Pos target = machine.Messages.First().target;
            int xx, yy, zz, aa, bb;

            xx = (((int)serial_buffer[3]) | (((int)serial_buffer[4]) << 8) | (((int)serial_buffer[5]) << 16) | (((int)serial_buffer[6]) << 24));
            machine.CurrentX = xx / Constants.XY_STEPS_PER_MM;
            yy = (((int)serial_buffer[7]) | (((int)serial_buffer[8]) << 8) | (((int)serial_buffer[9]) << 16) | (((int)serial_buffer[10]) << 24));
            machine.CurrentY = yy / Constants.XY_STEPS_PER_MM;
            zz = (((int)serial_buffer[11]) | (((int)serial_buffer[12]) << 8) | (((int)serial_buffer[13]) << 16) | (((int)serial_buffer[14]) << 24));
            machine.CurrentZ = zz / Constants.Z_STEPS_PER_MM;
            aa = (((int)serial_buffer[15]) | (((int)serial_buffer[16]) << 8) | (((int)serial_buffer[17]) << 16) | (((int)serial_buffer[18]) << 24));
            machine.CurrentA = aa / Constants.XY_STEPS_PER_MM;
            bb = (((int)serial_buffer[19]) | (((int)serial_buffer[20]) << 8) | (((int)serial_buffer[21]) << 16) | (((int)serial_buffer[22]) << 24));
            machine.CurrentB = bb / Constants.XY_STEPS_PER_MM;
            machine.LastEndStopState = ((int)serial_buffer[23]) | ((int)serial_buffer[24] << 8);

            if ((machine.CurrentX == lastPos.x) && (machine.CurrentY == lastPos.y) && (machine.CurrentZ == lastPos.z) && (machine.CurrentA == lastPos.a) && (machine.CurrentB == lastPos.b))
            {
                if ((Math.Abs(lastPos.x - target.x) < 1) || (target.axis & Constants.X_AXIS) == 0)
                {
                    if ((Math.Abs(lastPos.y - target.y) < 1) || (target.axis & Constants.Y_AXIS) == 0)
                    {
                        if ((Math.Abs(lastPos.z - target.z) < 1) || (target.axis & Constants.Z_AXIS) == 0)
                        {
                            if ((Math.Abs(lastPos.a - target.a) < 1) || (target.axis & Constants.A_AXIS) == 0)
                            {
                                if ((Math.Abs(lastPos.b - target.b) < 1) || (target.axis & Constants.B_AXIS) == 0)
                                {
                                    isGood = true;
                                }
                            }
                        }
                    }
                }
            }

            lastPos.x = machine.CurrentX; lastPos.y = machine.CurrentY; lastPos.z = machine.CurrentZ; lastPos.a = machine.CurrentA; lastPos.b = machine.CurrentB;
                       
            return isGood;
        }

       
    }

}

