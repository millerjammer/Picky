using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class OpticallyAlignToPartCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * This command uses the down camera to image the next Part in a Feeder.  It  
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
        public Feeder feeder;
        public CameraModel cameraToUse;

        public OpticallyAlignToPartCommand(Feeder _feeder )
        {
            machine = MachineModel.Instance;
            feeder = _feeder;
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
            machine.selectedPickListPart = feeder.Part;
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (feeder.Part.IsInView == true)
            {
                MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                nxt.target.x = feeder.NextPartOpticalLocation.X;
                nxt.target.y = feeder.NextPartOpticalLocation.Y;
                nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                return true;
            }
            return false;
        }
    }
}
