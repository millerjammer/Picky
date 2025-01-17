using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky.Commands
{
    public class SetCameraCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * Set Focus - includes a delay
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        public CameraSettings settings;
        public CameraModel camera;
        public long start_ms;

        public SetCameraCommand(CameraSettings _settings, CameraModel _camera)
        {
        /*-----------------------------------------------------
         * focus < 0 will be autoFocus
         *----------------------------------------------------*/

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Camera Settings\n");
            settings = _settings;
            camera = _camera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            MachineModel machine = MachineModel.Instance;
            Console.WriteLine("Camera Settings changed by SetCameraCommand");
            camera.Settings = settings.Clone();
            start_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > (start_ms + 2000)) 
                return true;
            return false;
        }
    }
}
