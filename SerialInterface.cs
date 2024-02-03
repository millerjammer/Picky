using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Interop;

/*For S3G Serial Commands See: https://github.com/makerbot/s3g/blob/master/doc/s3gProtocol.md */

namespace Picky
{
    public class SerialInterface 
    {
       private static byte[] serial_buffer = new byte[Constants.MAX_BUFFER_SIZE];
       private static int s_in = 0;
       private int tick = 0;

       MachineModel machine = MachineModel.Instance;

       private static MachineMessage.Pos lastPos = new MachineMessage.Pos();
       public int rx_msgCount { get; set; }
       public int tx_msgCount { get; set; }

       static SerialPort serialPort;

       public SerialInterface() {

            /* Open Port */
            serialPort = new SerialPort();
            serialPort.PortName = "COM12";
            serialPort.BaudRate = 115200;
            /* The following is critial! Or no RX for you */
            serialPort.RtsEnable = true;
            serialPort.DtrEnable = true;
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
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
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
                if (machine.isMachinePaused == false)
                {
                    ServiceOutboundMessageQueue();
                }
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
            bool isPositionGood = true;
            
            if (serialPort.IsOpen)
            {
                /**** Read the port, stick it at end of the main buffer *****/
                int avail = serialPort.BytesToRead;
                if (avail == 0)
                    return true;
                bytes_read = serialPort.Read(sbuf, 0, avail);
                for (i = 0; i < bytes_read; i++)
                {
                    serial_buffer[s_in++] = sbuf[i];
                }
                //Try to read a message 
                while (Array.IndexOf(serial_buffer, (byte)'\n') >= 0)
                {
                    length = Array.IndexOf(serial_buffer, (byte)'\n') + 1;

                    //There are enough bytes to parse a message, first, write what we got
                    for (j = 0; j < length; j++)
                        Console.Write((char)serial_buffer[j]);
                    Console.WriteLine();

                    //Check return message
                    if (serial_buffer[0] == (byte)'o' && serial_buffer[1] == (byte)'k')
                    {
                        rx_msgCount++;
                    }
                    else if (serial_buffer[0] == (byte)'X' && serial_buffer[1] == (byte)':')
                    {
                        rx_msgCount++;
                        isPositionGood = getPositionFromMessage();
                    }

                    //Fixup the byte buffer by moving unread bytes and the pointer
                    for (k = 0; k < Constants.MAX_BUFFER_SIZE - length; k++)
                    {
                        serial_buffer[k] = serial_buffer[k + length];
                    }
                    s_in = Array.IndexOf(serial_buffer, (byte)'\n') + 1;

                    //Fix up Message queue if position ok
                    if (isPositionGood == true && machine.Messages.Count > 0)
                    {
                        //Console.WriteLine("RX Count: " + rx_msgCount.ToString());
                        //Console.WriteLine("RX Data: " + rawString);
                        machine.Messages.RemoveAt(0);
                    }
                }
                return true;
            }
            return false;
        }

      
        private bool ServiceOutboundMessageQueue()
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
                    if (((machine.LastEndStopState & Constants.X_AXIS_MAX_SW) == Constants.X_AXIS_MAX_SW) && ((machine.LastEndStopState & Constants.Y_AXIS_MAX_SW) == Constants.Y_AXIS_MAX_SW))
                    {
                        machine.CalibrationStatusString = "Calibration XY: Successful";
                        machine.Messages.RemoveAt(0);
                    }
                    else
                    {
                        machine.PositionCalibrationState = MachineModel.CalibrationState.Failed;
                        machine.CalibrationStatusString = "Calibration XY: Failed (Timeout?)";
                        machine.Messages.Clear();
                    }
                }
                else if (msg.cmd[0] == Constants.JRM_SET_ABSOLUTE_XY_POSITION_OPTICALLY)
                {
                   Tuple<double, double> offset = machine.SelectedPickTool.GetPickOffsetAtRotation(0);
                   double pickX = msg.feeder.x_next_part + Constants.PICK_DISTORTION_OFFSET_X_MM - (offset.Item1 * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z));
                   double pickY = msg.feeder.y_next_part + Constants.PICK_DISTORTION_OFFSET_Y_MM - (offset.Item2 * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z));
                    if (msg.feeder.x_next_part != 0 && msg.feeder.y_next_part != 0)
                    {   
                        Console.WriteLine("Valid Imagery - Next Pick: " + pickX + " mm " + pickY + " mm");
                        MachineMessage next = GCommand.G_SetAbsolutePosition((byte)~(Constants.X_AXIS | Constants.Y_AXIS), pickX, pickY, 0, 0, 0);
                        machine.Messages.RemoveAt(0);
                        machine.Messages.Insert(0, next);
                    }
                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_CHECK_Z)
                {
                    Console.WriteLine("Calibration Z Check: " + machine.LastEndStopState);
                    if ((machine.LastEndStopState & (Constants.Z_AXIS_MIN_SW)) == (Constants.Z_AXIS_MIN_SW))
                    {
                        machine.PositionCalibrationState = MachineModel.CalibrationState.Complete;
                        machine.CalibrationStatusString = "Calibration Z: Successful";
                        machine.Messages.RemoveAt(0);
                    }
                    else
                    {
                        machine.PositionCalibrationState = MachineModel.CalibrationState.Failed;
                        machine.CalibrationStatusString = "Calibration Z: Failed (Timeout?)";
                        machine.Messages.Clear();
                    }
                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_ITEM_RESOLUTION)
                {
                    Console.WriteLine("In position, resolution cal started.");
                    tick = 10;
                    machine.Messages.Add(GCommand.JRM_CalibrationCalculateItemResolution1());
                    machine.Messages.RemoveAt(0);

                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_ITEM_RESOLUTION1)
                {
                    if (--tick == 0)
                    {
                        if ((machine.CalRectangle.Width < Constants.CALIBRATION_TARGET_WIDTH_DEFAULT_PIX + 80) && (machine.CalRectangle.Width > Constants.CALIBRATION_TARGET_WIDTH_DEFAULT_PIX - 80))
                        {
                            if ((machine.CalRectangle.Height < Constants.CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX + 80) && (machine.CalRectangle.Height > Constants.CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX - 80))
                            {
                                
                                Console.WriteLine("Final Target (w,h): " + machine.CalRectangle.Width + "px " + machine.CalRectangle.Height + "px");
                                Console.WriteLine("Item Resolution: X: " + machine.GetImageScaleAtDistanceX(machine.CurrentZ) + "mm/px Y:" + machine.GetImageScaleAtDistanceY(machine.CurrentZ) + "mm/px");
                                machine.CalibrationStatusString = "Item Resolution: Successful";
                                machine.SetCalRectangle(machine.CalRectangle);
                                machine.CameraCalibrationState = MachineModel.CalibrationState.Complete;
                                machine.Messages.RemoveAt(0);
                                /* Now head to a corner, change the z and compensate for focus */
                                double left_mm = machine.CurrentX - (((Constants.CAMERA_FRAME_WIDTH/2) - machine.CalRectangle.Left) * machine.GetImageScaleAtDistanceX(25));
                                double right_mm = machine.CurrentX + ((machine.CalRectangle.Right - (Constants.CAMERA_FRAME_WIDTH / 2)) * machine.GetImageScaleAtDistanceX(25));
                                double bottom_mm = machine.CurrentY - ((machine.CalRectangle.Bottom - (Constants.CAMERA_FRAME_HEIGHT / 2)) * machine.GetImageScaleAtDistanceY(25));
                                double top_mm = machine.CurrentY + (((Constants.CAMERA_FRAME_HEIGHT/2) - machine.CalRectangle.Top)  * machine.GetImageScaleAtDistanceY(25));
                                machine.Messages.Add(GCommand.G_SetAbsoluteZPosition(25.0));
                                machine.Messages.Add(GCommand.G_GetPosition());
                                machine.Messages.Add(GCommand.G_SetAbsoluteXYPosition(left_mm, top_mm));
                                machine.Messages.Add(GCommand.G_GetPosition()); 
                                machine.Messages.Add(GCommand.G_SetAbsoluteXYPosition(right_mm,top_mm));
                                machine.Messages.Add(GCommand.G_GetPosition());
                                machine.Messages.Add(GCommand.G_SetAbsoluteXYPosition(right_mm, bottom_mm));
                                machine.Messages.Add(GCommand.G_GetPosition());
                                machine.Messages.Add(GCommand.G_SetAbsoluteXYPosition(left_mm, bottom_mm));
                                machine.Messages.Add(GCommand.G_GetPosition());

                            }
                            else
                            {
                                machine.CameraCalibrationState = MachineModel.CalibrationState.Failed;
                                machine.CalibrationStatusString = "Item Resolution: Bad Height (Using default)";
                                machine.Messages.Clear();
                            }
                        }
                        else
                        {
                            machine.CameraCalibrationState = MachineModel.CalibrationState.Failed;
                            machine.CalibrationStatusString = "Item Resolution: Bad Width (Using default)";
                            machine.Messages.Clear();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Target Rect (w,h) [" + tick + "]: " + machine.CalRectangle.Width + "px " + machine.CalRectangle.Height + "px");
                    }
                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_CHECK_PICK)
                {
                    Console.WriteLine("Rotation Start");
                    machine.CalPick.Initialize();
                    //machine.Messages.Add(Command.S3G_SetAbsoluteAngle(50));
                    //machine.Messages.Add(Command.S3G_GetPosition());
                    //machine.Messages.Add(Command.S3G_SetAbsoluteAngle(100));
                    //machine.Messages.Add(Command.S3G_GetPosition());
                    //machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0));
                    //machine.Messages.Add(Command.S3G_GetPosition());
                    machine.Messages.Add(GCommand.JRM_CalibrationCalculatePick1());
                    machine.Messages.RemoveAt(0);
                }
                else if (msg.cmd[0] == Constants.JRM_CALIBRATION_CHECK_PICK1)
                {
                    Tuple<double, double> temp;
                    double radius_h, radius_x, offset_h, offset_x;
                    Console.WriteLine("Calculate Correction xmin: " + machine.CalPick.x_min + "@" + machine.CalPick.x_min_angle + " max: " + machine.CalPick.x_max + "@" + machine.CalPick.x_max_angle + " hmin: " + machine.CalPick.h_min + "@" + machine.CalPick.h_min_angle + " hmax: " + machine.CalPick.h_max + "@" + machine.CalPick.h_max_angle);
                    /* Show excentricity,from center of pick */
                    radius_x = (machine.CalPick.x_max - machine.CalPick.x_min) / 2;
                    radius_h = (machine.CalPick.h_max - machine.CalPick.h_min) / 2;
                    Console.WriteLine("Max X Variance: " + radius_x + "px " + (radius_x * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z)) + "mm ");
                    Console.WriteLine("Max H Variance: " + radius_x + "px " + (radius_x * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z)) + "mm ");
                    /* Show offset from center of image to center of pick uncertaintly circle */
                    offset_x = (radius_x + machine.CalPick.x_min);
                    offset_h = (radius_h + machine.CalPick.h_min);
                    Console.WriteLine("X Offset: " + offset_x + "px " + (offset_x * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z)) + "mm ");
                    Console.WriteLine("H Offset: " + offset_h + "px " + (offset_h * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z)) + "mm ");
                    Console.WriteLine("-- Angle -x-- Offset --y-- @ Z Park Pickup");
                    for (int i = 0; i < 360; i += 10)
                    {
                        temp = machine.SelectedPickTool.GetPickOffsetAtRotation(i);
                        Console.WriteLine("   " + i + "    " + (temp.Item1 * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z)) + " mm   " + (temp.Item2 * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z)) + " mm");
                    }
                    machine.CalibrationStatusString = "Pick Tool Cal: Successful";
                    machine.SetCalPickTool(machine.CalPick);
                    machine.PickCalibrationState = MachineModel.CalibrationState.Complete;
                    machine.Messages.RemoveAt(0);
                }
                else
                {
                    if(msg.part != null)
                    {
                        machine.selectedPickListPart = msg.part;
                        Console.WriteLine("using part: " + msg.part.Description);
                    }
                    int len = Array.IndexOf(msg.cmd, (byte)'\n');
                    Console.WriteLine("Write: " + Encoding.UTF8.GetString(msg.cmd));
                    serialPort.Write(msg.cmd, 0, len + 1);
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
            
            // Define a regular expression pattern for extracting doubles
            string pattern = @"(\d+\.\d+)";
            Regex regex = new Regex(pattern);

            // Match the pattern in the input string
            MatchCollection matches = regex.Matches(Encoding.UTF8.GetString(serial_buffer));

            machine.CurrentX = double.Parse(matches[0].Value);
            machine.CurrentY = double.Parse(matches[1].Value);
            machine.CurrentZ = double.Parse(matches[2].Value);
            
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

