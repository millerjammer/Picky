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
        
        public SetZProbeCalibrationCommand()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Z Probe Calibration\n");
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            MachineModel machine = MachineModel.Instance;
            machine.Cal.ZCalPadZ = machine.CurrentZ;
            Console.WriteLine("Z Cal: " + machine.Cal.ZCalPadZ);
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            return true;
        }
    }
}
