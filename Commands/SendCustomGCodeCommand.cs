using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class SendCustomGCodeCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES position of the next command to add an offset to align pick head  
    * Requires valid position in last command.
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        public CameraModel camera;
        public int delay;

        public SendCustomGCodeCommand(string gcode, int msDelay)
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.UTF8.GetBytes(string.Format("J102*{0}*\n", gcode));
            delay = (msDelay / Constants.QUEUE_SERVICE_INTERVAL);    //delay (ms) in units of service interval
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (delay-- > 0)
                return false;
            return true;
        }
    }
}