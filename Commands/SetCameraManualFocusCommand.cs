using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class SetCameraManualFocusCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES position of the next command to add an offset to align pick head  
    * Requires valid position in last command.
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        public CameraModel camera;
         public int delay;

        public SetCameraManualFocusCommand(CameraModel target_camera, bool enableMF, int value)
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Camera Focus\n");
            target_camera.IsManualFocus = enableMF;
            target_camera.Focus = value;
            delay = (400 / Constants.QUEUE_SERVICE_INTERVAL);    //delay (ms)
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
