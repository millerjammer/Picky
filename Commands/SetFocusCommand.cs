using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky.Commands
{
    public class SetFocusCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * Set Focus - includes a delay
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        public int valFocus;
        public int delay;

        public SetFocusCommand(int msDelay, int focus)
        {
        /*-----------------------------------------------------
         * focus < 0 will be autoFocus
         *----------------------------------------------------*/

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Focus\n");
            valFocus = focus;
            delay = msDelay / (5 * Constants.QUEUE_SERVICE_INTERVAL);    //delay (ms) in units of service interval
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            MachineModel machine = MachineModel.Instance;
            machine.downCamera.Settings.Focus = valFocus;
            if (valFocus == Constants.CAMERA_AUTOFOCUS)
                machine.downCamera.Settings.IsManualFocus = false;
            else
                machine.downCamera.Settings.IsManualFocus = true;
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (delay-- > 0)
            {
                Console.WriteLine("Delay: " + delay);
                return false;
            }
            return true;
        }
    }
}
