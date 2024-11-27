using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class SetScaleResolutionCalibrationCommand : MessageRelayCommand
    /*----------------------------------------------------------------------
     * This command requires a get best circle and a probe inorder to not be
     * reliant on a calibrated z, because, that's what we're trying to get.
     * OVERWRITES the call target.
     * --------------------------------------------------------------------*/
    {
        public MachineModel machine;
        public MachineMessage msg;
        public CircleDetector detector;
        public CalTargetModel calTarget;
        public CameraModel cameraToUse;

        public SetScaleResolutionCalibrationCommand(CalTargetModel ctm) 
        {
            machine = MachineModel.Instance;
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Scale Resolution Calibration\n");
            calTarget = ctm;
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            // Get the last best circle, first since our request will be based on a 
            // z position that we don't know.  Then, get the actual Z from the probe
            // and show if we're close.

            //OpenCvSharp.CircleSegment bestCircle = cameraToUse.GetBestCircle();
            //calTarget.SetMMToPixel(bestCircle.Radius);
            //calTarget.SetMMHeightZ(machine.CurrentZ);
            //Console.WriteLine("Scale Resolution Set: " + calTarget.MMPerPixX + " mm/pix @ " + calTarget.MMHeightZ + " mm");

            //var scale = machine.Cal.GetScaleMMPerPixAtZ(machine.CurrentZ + Constants.TOOL_LENGTH_MM);
            //double dia = (2 * bestCircle.Radius * scale.xScale) / Constants.MIL_TO_MM;
           // Console.WriteLine("Best Circle Check - Calibrated Diameter (1.000): " + dia);
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            return true;
        }
    }
}
