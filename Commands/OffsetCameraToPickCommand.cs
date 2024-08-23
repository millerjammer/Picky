using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class OffsetCameraToPickCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES position of the next command to add an offset to align pick head  
    * Requires valid position in last command.
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        
        public OffsetCameraToPickCommand()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Offset Camera to Pick\n");
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
            MachineMessage message = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) - 1);
            //Get offset in mm and add to last target
            var offset = machine.Cal.GetPickHeadOffsetToCameraAtZ(0);
            double x = message.target.x + offset.x_offset;
            double y = message.target.y + offset.y_offset;
            //Update next command
            message = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
            message.target.x = x;
            message.target.y = y;
            message.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", message.target.x, message.target.y));
            return true;
        }
    }
}