using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class DelayCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES position of the next command to add an offset to align pick head  
    * Requires valid position in last command.
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        public long delay;
        public long start_ms;

        public DelayCommand(int msDelay)
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Simple Delay\n");
            delay = msDelay;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            start_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > (start_ms + delay))
                return true;
            return false;
        }
    }
}
