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
        public Part part;
        public double rotation; //Used for correction only
        
        public OffsetCameraToPickCommand(double head_rotation)
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Offset Camera to Pick\n");
            rotation = head_rotation;
        }

        public OffsetCameraToPickCommand(Part _part)
        {
            /* Part is based so that it's selected in the Pick List and Feeder */
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Offset Camera to Pick\n");
            part = _part;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            if (part != null)
            {
                MachineModel machine = MachineModel.Instance;
                machine.selectedPickListPart = part;
                if (part.cassette != null)
                {
                    machine.selectedCassette = part.cassette;
                    machine.selectedCassette.selectedFeeder = part.feeder;
                }
            }
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            /* Move head from Camera to Pick */
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