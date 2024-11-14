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
        public double targetZ;
        public Part part;

        public OffsetCameraToPickCommand(Part _part, double _targetZ)
        {
            /* Part is based so that it's selected in the Pick List and Feeder */
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Offset Camera to Pick\n");
            part = _part;
            targetZ = _targetZ;
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
            Console.WriteLine("start");
            /* Move head from Camera to Pick */
            MachineModel machine = MachineModel.Instance;
            MachineMessage message = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) - 1);

            //Get offset in pixels and add to last target
            //Point2d offset = GetCalibratedOffset(targetZ, double.Parse(part.Rotation));
            //Convert to mm
            var scale = machine.Cal.GetScaleMMPerPixAtZ(targetZ);
           // double x = message.target.x + (scale.xScale * offset.X); // machine.Cal.OriginToDownCameraX;
            //double y = message.target.y + (scale.yScale * offset.Y); // machine.Cal.OriginToDownCameraY;


            //Update next command
           // Console.WriteLine("Part: x:" + message.target.x + "mm ," + message.target.y + "mm");
            //Console.WriteLine("For Pick: x:" + x + "mm ," + y + "mm");
            //message = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
           // message.target.x = x;
           // message.target.y = y;
            //message.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", message.target.x, message.target.y));
            Console.WriteLine("Done");
            return true;
        }
    }
}