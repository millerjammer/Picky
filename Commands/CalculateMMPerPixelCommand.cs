using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class CalculateMMPerPixelCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES MM Per Pixel 
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;

        public CalculateMMPerPixelCommand()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Calculate MM Per Pixel\n");
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
            MachineModel machine = MachineModel.Instance;
            machine.Cal.CalTarget.CalculateMachineMMPerPixel();
            return true;
        }
    }
}
