using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class SetToolLengthCommand : MessageRelayCommand
    /*----------------------------------------------------------------------
     * This command measures the length of a tool.
     * --------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        private PickToolModel Tool;
        private int delay;

        public SetToolLengthCommand(PickToolModel tool)
        {
            Tool = tool;
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Length\n");
            delay = (200 / Constants.QUEUE_SERVICE_INTERVAL);
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
            if(delay-- > 0)
                return false;
            MachineModel machine = MachineModel.Instance;
            if (machine.CurrentX == machine.Cal.CalPad.X && machine.CurrentY == machine.Cal.CalPad.Y)
            {
                Tool.Length = machine.Cal.CalPad.Z - machine.CurrentZ;
                Console.WriteLine("Tool Length: Success, length: " + Tool.Length + "mm");
            }
            else
            {
                Console.WriteLine("Tool Length: Fail, bad location");
            }
            return true;
        }
    }
}
