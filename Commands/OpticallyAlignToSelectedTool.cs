using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class SetPickOffsetCalibrationCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * This command uses the lower camera to image the tip of the tool and calculate
     * it's position at various angles.  The result is placed in the tool's calibration
     * and is used as an offset for each pick and place action.  This command should run after
     * every tool change.
     * 
     * REQUIREMENTS:
     *      Head must be positioned at 0,0
     *      Up Illuminator 'On'
     *      Down Illuminator 'Off'
     *      Valid calibrations
     *-------------------------------------------------------------------------------*/

    {
        private MachineModel machine;

        public SetPickOffsetCalibrationCommand(MachineModel mm) 
        {
            machine = mm;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (msg.cameraToUse.IsCircleSearchActive() == false)
            {
                //Get offset in mm
                var scale = machine.Cal.GetScaleMMPerPixAtZ(35.56);
                OpenCvSharp.CircleSegment bestCircle = msg.cameraToUse.GetBestCircle();
                double x_offset = scale.xScale * bestCircle.Center.X;
                double y_offset = scale.yScale * bestCircle.Center.Y;
                double radius = scale.yScale * bestCircle.Radius;
                if (radius < 0.1)
                {
                    Console.WriteLine("PickOffset Failed, Repeating Request.");
                    msg.cameraToUse.RequestCircleLocation(msg.roi, msg.circleToFind);
                    return false;
                }
                else
                    Console.WriteLine("PickOffset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                return true;
            }
            return false;
        }
    }

    
}
