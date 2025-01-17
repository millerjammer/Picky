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
        public Position3D pos_to_update;
        private int delay;
        
        public GetZProbeCommand(Position3D z_pos_to_update)
        {
            pos_to_update = z_pos_to_update;
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Calibration\n");
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
            pos_to_update.Z = machine.Current.Z;
            Console.WriteLine("Last Z Result: " + machine.Current.Z + "mm");
            return true;
        }
    }
}
