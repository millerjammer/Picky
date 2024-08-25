using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class StepAlignToCalCircleCommand : MessageRelayCommand
    /*---------------------------------------------------------------
     * This function moves the head to a circle that looks like the
     * 'estimated' circle using iteration.  This is used when you don't 
     * know mm/pixel at the current Z.  This is typically used for 
     * calibration only.  Resolution calibration must be set first. 
     * OVERWRITES the position of the next command.  This command must 
     * be followed by a position command.
     * -------------------------------------------------------------*/
    {
        public MachineModel machine;
        public MachineMessage msg;
        public CircleDetector detector;
        public CameraModel cameraToUse;
        public Circle3d dest;

        public StepAlignToCalCircleCommand(MachineModel mm, CircleSegment target, Circle3d destination)
        {
            machine = mm;
            detector = new CircleDetector(HoughModes.Gradient, 140, 50);
            if (target.Radius > 5)
                detector.ROI = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            else
                detector.ROI = new OpenCvSharp.Rect(Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            detector.CircleEstimate = target;
            detector.zEstimate = 52;
            msg = new MachineMessage();
            msg.target.x = target.Center.X; msg.target.y = target.Center.Y;
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Step Align To Calibration Circle\n");
            cameraToUse = machine.downCamera;
            dest = destination;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }
        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.RequestCircleLocation(detector);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                //Take a guess of the offset, this will be rewritten once successful
                double scale = .0206;
                CircleSegment cir = cameraToUse.GetBestCircle(); //In pixels
                double x_offset = scale * cir.Center.X;
                double y_offset = scale * cir.Center.Y;
                double radius = scale * cir.Radius;
                
                if (radius > (detector.CircleEstimate.Radius * 2.0) || radius < (detector.CircleEstimate.Radius * .5) )
                {
                    Console.WriteLine("Cal Target Alignment Failed, Repeating Request. Radius: " + radius + " mm");
                    cameraToUse.RequestCircleLocation(detector);
                    return false;
                }
                else
                {
                    Console.WriteLine("Cal Circle Offset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                    // Use 1/2 the offset to calculate next location, we can't assume to know resolution, so we can't jump 
                    nxt.target.x = machine.CurrentX - (float)(x_offset * 0.8);
                    nxt.target.y = machine.CurrentY + (float)(y_offset * 0.8);
                    nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                    if (dest != null)
                    {
                        dest.X = nxt.target.x;
                        dest.Y = nxt.target.y;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
