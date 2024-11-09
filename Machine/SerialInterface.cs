using EnvDTE;
using OpenCvSharp;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using static Picky.MachineMessage;

namespace Picky
{
    public class SerialInterface : INotifyPropertyChanged
    {
        private static byte[] serial_buffer = new byte[Constants.MAX_BUFFER_SIZE];
        private static int s_in = 0;

        MachineModel machine = MachineModel.Instance;

        private static MachineMessage.Pos lastPos = new MachineMessage.Pos();
        
        private string comPortStatus;
        public string ComPortStatus
        {
            get { return comPortStatus; }
            set { comPortStatus = value; OnPropertyChanged(nameof(ComPortStatus)); }
        }

        private string comPortName = Constants.SERIAL_PORT;
        public string ComPortName
        {
            get {  return comPortName;}
            set { comPortName = value; OnPropertyChanged(nameof(ComPortName));}
        }

        static SerialPort serialPort;
        System.Timers.Timer msgTimer;

        public SerialInterface()
        {   
            /* Create Serial Port */
            serialPort = new SerialPort();
            serialPort.PortName = ComPortName;
            serialPort.BaudRate = 115200;
            /* The following is critial! Or no RX for you */
            serialPort.RtsEnable = true;
            serialPort.DtrEnable = true;
            serialPort.PinChanged += SerialPort_PinChanged;
            
            /* Start a Timer to Handle Message Queue */
            msgTimer = new System.Timers.Timer();
            msgTimer.Elapsed += new ElapsedEventHandler(OnTimer);
            msgTimer.Interval = Constants.QUEUE_SERVICE_INTERVAL;
            msgTimer.Enabled = true;
        }

        public void Dispose()
        {
            serialPort.Close();
            msgTimer.Stop();
            msgTimer.Close();
            msgTimer.Dispose();

        }

        private bool StartSerialPortCommunication() { 
           
            try
            {
                /* Initial Comm */
                Application.Current.Dispatcher.Invoke(() =>
                {
                    machine.RxMessageCount = 0;
                    machine.Messages.Clear();
                });
                serialPort.Open();
            }
            catch (Exception exc)
            {
                ComPortStatus = string.Format("Disconnected: {0}", ComPortName);
                Console.WriteLine("Couldn't Open Port:" + exc.Message);
                return false;
            }
            if (serialPort.IsOpen)
            {
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.ReadExisting();
                ComPortStatus = string.Format("Connected: {0}", ComPortName);
                Console.WriteLine("Machine Serial Port Opened. " + System.Environment.CurrentDirectory);
            }
            
            
            /* Initial Command Queue */
            Application.Current.Dispatcher.Invoke(() =>
            {
                machine.Messages.Add(GCommand.G_SetAutoPositionReporting(true));
                machine.Messages.Add(GCommand.G_EnableIlluminator(true));
                machine.Messages.Add(GCommand.G_GetStepsPerUnit());
                machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
                machine.Messages.Add(GCommand.G_SetZPosition(0));
                machine.Messages.Add(GCommand.G_SetXYBacklashCompensationOff());
            });
            return true;
        }

        private void SerialPort_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            if (e.EventType == SerialPinChange.CDChanged || e.EventType == SerialPinChange.DsrChanged || e.EventType == SerialPinChange.Break)
            {
                // Handle the disconnection if the DCD or DSR pins change state
                Console.WriteLine("Serial port disconnected.");
                ComPortStatus = string.Format("Disconnected: {0}", ComPortName);
                // Perform cleanup or reconnection logic as necessary
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    Console.WriteLine("Serial port closed.");
                    ComPortStatus = string.Format("Port Closed: {0}", ComPortName);
                }
            }
        }
              

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
               

        public void OnTimer(object source, ElapsedEventArgs e)
        /************************************************************
         * Yes, it's all handled on a timer rather than when data comes
         * in.  This reduces overhead since it's more likely there's a 
         * complete message in the queue to process. 
         * **********************************************************/
        {
            if (serialPort.IsOpen)
            {
                ServiceInboundMessages();
                if (machine.Messages.Count() > 0 && machine.Messages.Count() > machine.RxMessageCount)
                {
                    MachineMessage msg = machine.Messages.ElementAt(machine.RxMessageCount);
                    ServiceOutboundMessage(msg);
                }
            }
            else
            // Port not open try to re-open on slower interval
            {
                if (StartSerialPortCommunication())
                {
                    msgTimer.Interval = Constants.QUEUE_SERVICE_INTERVAL;
                }
                else
                {
                    msgTimer.Interval = Constants.OPEN_PORT_INTERVAL;
                }
            }
        }

        private bool ServiceInboundMessages()
        /********************************************************************
         * Checks and processes serial data from the machine.
         * Returns true if a command was successfully parsed.
         *********************************************************************/
        {

            int length, i, j, k;
            int bytes_read;
            MachineMessage msg;
            byte[] sbuf = new byte[4096];  //17 bits

            if (serialPort.IsOpen)
            {
                /**** Read the port, stick it at end of the main buffer *****/
                bytes_read = serialPort.Read(sbuf, 0, serialPort.BytesToRead);
                for (i = 0; i < bytes_read; i++)
                    serial_buffer[s_in++] = sbuf[i];

                //Try to read messages if we got a \n in the buffer
                while (Array.IndexOf(serial_buffer, (byte)'\n') >= 0)
                {
                    length = Array.IndexOf(serial_buffer, (byte)'\n') + 1;

                    //There are enough bytes to parse a message, first, write what we got
                    //for (j = 0; j < length; j++)
                    //Console.Write((char)serial_buffer[j]);

                    //Check return message, OK message is always the last to send
                    if (machine.Messages.Count > 0 && machine.Messages.Count > machine.RxMessageCount)
                    {
                        msg = machine.Messages.ElementAt(machine.RxMessageCount);
                        if (msg.state == MachineMessage.MessageState.PendingOK)
                        {
                            if (serial_buffer[0] == (byte)'o' && serial_buffer[1] == (byte)'k')
                            {
                                for (j = 0; j < length; j++)
                                    Console.Write((char)serial_buffer[j]);
                                // Parse Steps per Unit
                                if (msg.cmdString.StartsWith("M92") || msg.cmdString.StartsWith("M503"))
                                {
                                    // The OK comes out AFTER the result - result parsed below
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                                // Parse Illuminator Status
                                else if (msg.cmdString.StartsWith("M260"))
                                {
                                    string pattern = @"B(\d{1,3})";     // Find first B0 (device) then capture the value
                                    Regex regex = new Regex(pattern);
                                    MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(msg.cmd));
                                    if (matches[1].Value == "B0" && matches[2].Value == "B128")
                                    {
                                        Console.WriteLine("down illuminator off");
                                        machine.IsIlluminatorActive = false;
                                    }
                                    else if (matches[1].Value == "B0" && matches[2].Value == "B178")
                                    {
                                        Console.WriteLine("down illuminator on");
                                        machine.IsIlluminatorActive = true;
                                    }
                                    // Ack all I2C messages
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                                // Parse Fan Status
                                else if (msg.cmdString.StartsWith("M106"))
                                {
                                    string pattern = @"(?<=[P|S])\d+";     // Find first P[device] then capture S[value]
                                    Regex regex = new Regex(pattern);
                                    MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(msg.cmd));
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                    if (matches[0].Value == "0" && matches[1].Value == "0")
                                    {
                                        machine.IsValveActive = false;
                                    }
                                    else if (matches[0].Value == "0" && matches[1].Value == "255")
                                    {
                                        machine.IsValveActive = true;
                                    }
                                }
                                // Parse Servo Status
                                else if (msg.cmdString.StartsWith("M280"))
                                {
                                    string pattern = @"(?<=[P|S])\d+";     // Find first P[device] then capture S[value]
                                    Regex regex = new Regex(pattern);
                                    MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(msg.cmd));
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                    if (matches[0].Value == "0" && matches[1].Value == "68")
                                    {
                                        machine.IsToolStorageOpen = true;
                                    }
                                    else if (matches[0].Value == "0" && matches[1].Value == "9")
                                    {
                                        machine.IsToolStorageOpen = false;
                                    }
                                }
                                // Parse Pin Set State Command
                                else if (msg.cmdString.StartsWith("M42 "))
                                {
                                    string pattern = @"(?<=[P|S])\d+";
                                    Regex regex = new Regex(pattern);
                                    MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(msg.cmd));
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                    if (matches[0].Value == "203")
                                    {
                                        if (matches[1].Value == "0")
                                        {
                                            machine.IsValveActive = false;
                                            Console.WriteLine("valve off");
                                        }
                                        else
                                        {
                                            machine.IsValveActive = true;
                                            Console.WriteLine("valve on");
                                        }
                                    }
                                    else if (matches[0].Value == "207")
                                    {
                                        if (matches[1].Value == "0")
                                        {
                                            machine.IsPumpActive = false;
                                            Console.WriteLine("pump off");
                                        }
                                        else
                                        {
                                            machine.IsPumpActive = true;
                                            Console.WriteLine("pump on");
                                        }
                                    }
                                    else if (matches[0].Value == "204")
                                    {
                                        if (matches[1].Value == "0")
                                        {
                                            machine.IsUpIlluminatorActive = false;
                                            Console.WriteLine("up illuminator off");
                                        }
                                        else
                                        {
                                            machine.IsUpIlluminatorActive = true;
                                            Console.WriteLine("up illuminator on");
                                        }
                                    }
                                }
                                else if (msg.cmdString.StartsWith("G90"))
                                {

                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                                else if (msg.cmdString.StartsWith("G91"))
                                {

                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                                else
                                {
                                    //Some message we don't care about, mark it complete
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                            }
                            // Handle commands do not output an OK to the serial port - these are mostly camera commands
                            else if (msg.cmdString.StartsWith("J102"))
                            {
                                if (msg.messageCommand.PostMessageCommand(msg) == true)
                                {
                                    msg.state = MachineMessage.MessageState.Complete;
                                    machine.RxMessageCount++;
                                }
                                if (msg.cmd[4] == '*')
                                {
                                    machine.Settings.Response += Encoding.UTF8.GetString(serial_buffer, 0, Array.IndexOf(serial_buffer, (byte)'\n') + 1);
                                }
                            }
                            //Message "M503" output comes befor OK ends with ok
                            else if (msg.cmdString.StartsWith("M503"))
                            {
                                machine.Settings.Response += Encoding.UTF8.GetString(serial_buffer, 0, Array.IndexOf(serial_buffer, (byte)'\n') + 1);
                            }
                            
                        }
                        // Messages waiting for valid position = Target position
                        if (msg.state == MachineMessage.MessageState.PendingPosition && isPositionGood(msg))
                        {
                            msg.state = MachineMessage.MessageState.Complete;
                            machine.RxMessageCount++;
                        }
                        //Get Steps Response from query ("M92") ok comes AFTER DATA
                        else if (serial_buffer[2] == (byte)'M' && serial_buffer[3] == (byte)'9' && serial_buffer[4] == (byte)'2')
                        {
                            string pattern = @"(\d+\.\d+)";     // Define a regular expression pattern for extracting doubles
                            Regex regex = new Regex(pattern);
                            MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(serial_buffer));
                            machine.Cal.StepsPerUnitX = double.Parse(matches[0].Value);
                            machine.Cal.StepsPerUnitY = double.Parse(matches[1].Value);
                            Console.WriteLine("Steps per MM: " + machine.Cal.StepsPerUnitX + " " + machine.Cal.StepsPerUnitY);
                        }
                    }

                    //Handle Unsolicited Messages
                    if (serial_buffer[0] == (byte)'X' && serial_buffer[1] == (byte)':')
                    {
                        getPositionFromMessage();
                    }
                    
                    //Fixup the byte buffer by moving unread bytes and the pointer
                    for (k = 0; k < Constants.MAX_BUFFER_SIZE - length; k++)
                    {
                        serial_buffer[k] = serial_buffer[k + length];
                    }
                    s_in = Array.IndexOf(serial_buffer, (byte)'\n') + 1;
                }
                return true;
            }
            return false;
        }
        


            private bool ServiceOutboundMessage(MachineMessage msg)
            /********************************************************************
            * This is outbound messages to the machine.  If there is still a pending 
            * message, no new message is sent
            * Returns 'true' if a message was sent.
            *********************************************************************/
            {

                if (msg.state == MachineMessage.MessageState.Complete)
                    return false;
                if (msg.state == MachineMessage.MessageState.ReadyToSend)
                {
                    machine.SelectedMachineMessage = machine.Messages.ElementAt(machine.Messages.IndexOf(msg));
                    if (machine.isMachinePaused == false || machine.advanceNextMessage == true)
                    {
                        msg.delay -= Constants.QUEUE_SERVICE_INTERVAL;
                        if (msg.delay <= 0)
                        {
                            msg.state = MachineMessage.MessageState.PendingDelay;
                        }
                    }
                }
                if (msg.state == MachineMessage.MessageState.PendingDelay)
                {
                    if (machine.isMachinePaused == false || machine.advanceNextMessage == true)
                        msg.delay -= Constants.QUEUE_SERVICE_INTERVAL;

                    if (msg.delay <= 0)
                    {
                        // Set message start time
                        DateTimeOffset now = DateTimeOffset.UtcNow;
                        msg.start_time = now.ToUnixTimeMilliseconds();
                        // Send Message, if this is a G-Code
                        if ((msg.cmd[0] == 'G') || (msg.cmd[0] == 'M'))
                        {
                            int len = Array.LastIndexOf(msg.cmd, (byte)'\n');
                            serialPort.Write(msg.cmd, 0, len + 1);
                        }
                        else if (msg.cmdString.StartsWith("J102"))
                        {
                            msg.messageCommand.PreMessageCommand(msg);
                            if (msg.cmd[4] == '*')
                            {
                                int len = Array.LastIndexOf(msg.cmd, (byte)'*');
                                msg.cmd[len] = (byte)'\n';
                                serialPort.Write(msg.cmd, 5, len + 1 - 5);
                            }
                        }
                        // Reset the Next button
                        machine.advanceNextMessage = false;
                        msg.state = MachineMessage.MessageState.PendingOK;
                        msg.delay = 0;
                    }
                }
                return true;
            }

            private bool getPositionFromMessage()
            /********************************************************************
             * Helper - Updates current position and message
             *********************************************************************/
            {
                // Define a regular expression pattern for extracting doubles
                string pattern = @"(\d+\.\d+)";
                Regex regex = new Regex(pattern);

                // Match the pattern in the input string
                MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(serial_buffer));

                machine.CurrentX = double.Parse(matches[0].Value);
                machine.CurrentY = double.Parse(matches[1].Value);
                machine.CurrentZ = double.Parse(matches[2].Value);
                machine.CurrentA = double.Parse(matches[3].Value);

                return true;
            }

            private bool isPositionGood(MachineMessage msg)
            /********************************************************************
            * Helper - Checks if the machine has arrived and location is stable. 
            * Generally used by morph commands that can't use FinishMoves()
            *********************************************************************/
            {
                bool pOK = false;

                // Make sure machine has stopped
                if ((machine.CurrentX == lastPos.x) && (machine.CurrentY == lastPos.y) && (machine.CurrentZ == lastPos.z) && (machine.CurrentA == lastPos.a) && (machine.CurrentB == lastPos.b))
                {
                    if ((Math.Abs(lastPos.x - msg.target.x) < 1))
                    {
                        if ((Math.Abs(lastPos.y - msg.target.y) < 1))
                        {
                            if ((Math.Abs(lastPos.z - msg.target.z) < 20))
                            {
                                if ((Math.Abs(lastPos.a - msg.target.a) < 1))
                                {
                                    if ((Math.Abs(lastPos.b - msg.target.b) < 1))
                                    {
                                        pOK = true;
                                    }
                                }
                            }
                        }
                    }
                }

                lastPos.x = machine.CurrentX; lastPos.y = machine.CurrentY; lastPos.z = machine.CurrentZ; lastPos.a = machine.CurrentA; lastPos.b = machine.CurrentB;
                return pOK;
            }
        }
    }


