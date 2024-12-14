using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class GetZProbeCommand : MessageRelayCommand
    /*----------------------------------------------------------------------
     * This command writes the current Z location the Z Probe Calibration 
     * field.
     * --------------------------------------------------------------------*/
    {
        public MachineMessage msg;
        private int delay;
        
        public GetZProbeCommand(double distance_mm)
        {
            msg = GCommand.G_ProbeZ(distance_mm);
            msg.messageCommand = this;
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
            machine.LastZProbeResult = machine.CurrentZ;
            if (machine.CurrentX == machine.Cal.CalPad.X && machine.CurrentY == machine.Cal.CalPad.Y)
            {
                machine.Cal.CalPad.Z = machine.CurrentZ;
                Console.WriteLine("Calibrated Z Location: Success");
            }
            Console.WriteLine("Last Z Result: " + machine.LastZProbeResult + "mm");
            return true;
        }
    }
}
