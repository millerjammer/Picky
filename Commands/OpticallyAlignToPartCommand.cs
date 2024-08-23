using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class OpticallyAlignToPartCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * This command uses the down camera to image the next part in a feeder.  It  
    * OVERWRITES position of the next command to add an offset to align camere.  
    * It does NOT do the camera to head offset.
    * 
    * REQUIREMENTS:
    *      Up Illuminator 'Off'
    *      Down Illuminator 'On'
    *      Valid mmm/pixel calibration
    *      Next command needs to be a position command, it will be overwritten
    *-------------------------------------------------------------------------------*/
    {

        public MachineModel machine;
        public MachineMessage msg;
        public Part part;
        public CameraModel cameraToUse;

        public OpticallyAlignToPartCommand(Part prt )
        {
            machine = MachineModel.Instance;
            part = prt;            
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Optically Align to Part\n");
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            machine.selectedPickListPart = part;
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (part.IsInView == true)
            {
                //Get offset in mm
                Point2d offset = cameraToUse.GetNextPartOffset();
                var scale = machine.Cal.GetScaleMMPerPixAtZ( 50 + 11.5 );
                double x_location_mm =  machine.CurrentX - (scale.xScale * offset.X);
                double y_location_mm =  machine.CurrentY + (scale.yScale * offset.Y);
                Console.WriteLine("Part Location: " + x_location_mm + " mm " + y_location_mm + " mm");
                //Update next command
                MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                nxt.target.x = x_location_mm;
                nxt.target.y = y_location_mm;
                nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                return true;
            }
            return false;
        }
    }
}
