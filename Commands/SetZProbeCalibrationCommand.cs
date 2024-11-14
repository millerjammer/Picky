using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class SetZProbeCalibrationCommand : MessageRelayCommand
    /*----------------------------------------------------------------------
     * This command writes the current Z location the Z Probe Calibration 
     * field.
     * --------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        private int delay;
        
        public SetZProbeCalibrationCommand()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Z Probe Calibration\n");
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
            if (delay-- > 0)
                return false;
            MachineModel machine = MachineModel.Instance;
            if (machine.CurrentX == machine.Cal.ZCalPadX && machine.CurrentY == machine.Cal.ZCalPadY)
            {
                machine.Cal.ZCalPadZ = machine.CurrentZ;
                Console.WriteLine("Z Calibration: Success");
            }
            else
            {
                Console.WriteLine("Z Calibration: Fail, bad location");
            }
            Console.WriteLine("Z Cal: " + machine.Cal.ZCalPadZ);
            return true;
        }
    }
}
