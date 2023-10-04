using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Interop;
namespace Picky
{
    
    internal class Machine : INotifyPropertyChanged
    {
        private static byte[]  serial_buffer = new byte[Constants.MAX_BUFFER_SIZE];
        private static int s_in = 0;

        private static MachineMessage.Pos lastPos = new MachineMessage.Pos();
        public Queue<MachineMessage> messages;
        public int rx_msgCount { get; set; }
        public int tx_msgCount { get; set; }

        /* These are all in MM */
        private double C_X;
        public double currentX
        {
            get { return C_X; }
            set { C_X = value; PropertyChanged(this, new PropertyChangedEventArgs("currentX")); }
        }
        private double C_Y;
        public double currentY
        {
            get { return C_Y; }
            set { C_Y = value; PropertyChanged(this, new PropertyChangedEventArgs("currentY")); }
        }
        private double C_Z;
        public double currentZ
        {
            get { return C_Z; }
            set { C_Z = value; PropertyChanged(this, new PropertyChangedEventArgs("currentZ")); }
        }
        private double C_A;
        public double currentA
        {
            get { return C_A; }
            set { C_A = value; PropertyChanged(this, new PropertyChangedEventArgs("currentA")); }
        }
        private double C_B;
        public double currentB
        {
            get { return C_B; }
            set { C_B = value; PropertyChanged(this, new PropertyChangedEventArgs("currentB")); }
        }

        static SerialPort serialPort;

        public event PropertyChangedEventHandler PropertyChanged;

        public Machine()
        /************************************************************
         * Machine serial port and message queue management
         * 
         * **********************************************************/
        {
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
                messages = new Queue<MachineMessage>();
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
                for(i=0;i<bytes_read; i++)
                {
                    serial_buffer[s_in++] = sbuf[i];
                }
                //Try to read a message 
                for (i = 0; i < s_in; i++) {
                    if (serial_buffer[i] == (byte)Constants.SS_MIGHTBOARD_HEADER)
                    {
                        length = serial_buffer[i + 1] + 3;
                        if (length <= (s_in - i)) {
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
                            for (k = 0; k < Constants.MAX_BUFFER_SIZE-length; k++)
                            {
                                serial_buffer[k] = serial_buffer[i + length];
                            }
                            s_in -= i + length;
                            if (isPositionGood == true && messages.Count > 0)
                            {
                                Console.WriteLine("RX Count: " + rx_msgCount.ToString());
                                Console.WriteLine("RX Data: " + rawString);
                                messages.Dequeue();
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
            if (messages.Count() > 0)
            {
                MachineMessage msg = messages.First();
                serialPort.Write(msg.cmd, 0, msg.cmd[1] + 3);
                if (msg.target.axis != 0)
                {
                    //dlg.SetActivePickLocation(messages.front().target);
                }
                tx_msgCount++;
            }
            return true;
        }

        private bool getPositionFromMessage()
        /********************************************************************
         * Helper - Updates current position
         *
         *********************************************************************/
        {
            /*
            * Return true if position is good (close to target position)
            */
            bool isGood = false;
            if (messages.Count == 0)
            {
                //If the queue is empty
                //dlg.m_messageBox.AddString(L"Get Position Request: Queue Empty.");
                return true;
            }
            MachineMessage.Pos target = messages.First().target; 
            int xx, yy, zz, aa, bb;

            xx = (((int)serial_buffer[3]) | (((int)serial_buffer[4]) << 8) | (((int)serial_buffer[5]) << 16) | (((int)serial_buffer[6]) << 24));
            currentX = xx / Constants.XY_STEPS_PER_MM;
            yy = (((int)serial_buffer[7]) | (((int)serial_buffer[8]) << 8) | (((int)serial_buffer[9]) << 16) | (((int)serial_buffer[10]) << 24));
            currentY = yy / Constants.XY_STEPS_PER_MM;
            zz = (((int)serial_buffer[11]) | (((int)serial_buffer[12]) << 8) | (((int)serial_buffer[13]) << 16) | (((int)serial_buffer[14]) << 24));
            currentZ = zz / Constants.Z_STEPS_PER_MM;
            aa = (((int)serial_buffer[15]) | (((int)serial_buffer[16]) << 8) | (((int)serial_buffer[17]) << 16) | (((int)serial_buffer[18]) << 24));
            currentA = aa / Constants.XY_STEPS_PER_MM;
            bb = (((int)serial_buffer[19]) | (((int)serial_buffer[20]) << 8) | (((int)serial_buffer[21]) << 16) | (((int)serial_buffer[22]) << 24));
            currentB = bb / Constants.XY_STEPS_PER_MM;

            if ((currentX == lastPos.x) && (currentY == lastPos.y) && (currentZ == lastPos.z) && (currentA == lastPos.a) && (currentB == lastPos.b))
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

            lastPos.x = currentX; lastPos.y = currentY; lastPos.z = currentZ; lastPos.a = currentA; lastPos.b = currentB;

            return isGood;
        }
    }
}
