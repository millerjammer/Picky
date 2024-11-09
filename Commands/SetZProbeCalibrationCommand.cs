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
        public Point2d Location;
        private int delay;
        
        public SetZProbeCalibrationCommand(Point2d location)
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Z Probe Calibration\n");
            delay = (200 / Constants.QUEUE_SERVICE_INTERVAL);
            Location = location;
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
            if(Location.X == machine.Cal.ZCalPadX && Location.Y == machine.Cal.ZCalPadY)
                machine.Cal.ZCalPadZ = machine.CurrentZ;
            else if (Location.X == machine.Cal.ZCalDeckPadX && Location.Y == machine.Cal.ZCalDeckPadY)
                machine.Cal.ZCalDeckPadZ = machine.CurrentZ;
            Console.WriteLine("Z Cal: " + machine.Cal.ZCalPadZ + " @ " + Location.ToString());
            return true;
        }
    }
}
