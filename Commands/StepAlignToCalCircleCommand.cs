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
     * -------------------------------------------------------------*/
    {
        public MachineModel machine;
        public MachineMessage msg;
        public OpenCvSharp.Rect roi;
        public CircleSegment calTarget;
        public CameraModel cameraToUse;
        public double targetZ;
        public Circle3d dest;

        public StepAlignToCalCircleCommand(MachineModel mm, CircleSegment target, double tZ, Circle3d destination)
        {
            machine = mm; 
            if(target.Radius > 5)
                roi = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            else
                roi = new OpenCvSharp.Rect(Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            calTarget = target;
            msg = new MachineMessage();
            targetZ = tZ;
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Step Align To Calibration Circle\n");
            msg.target.x = target.Center.X; msg.target.y = target.Center.Y;
            cameraToUse = machine.downCamera;
            dest = destination;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }
        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.RequestCircleLocation(roi, calTarget, targetZ);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                //Take a guess of the offset, this will be rewritten once successful
                var scale = machine.Cal.GetScaleMMPerPixAtZ(targetZ);
                CircleSegment cir = cameraToUse.GetBestCircle(); //In pixels
                double x_offset = scale.xScale * cir.Center.X;
                double y_offset = scale.yScale * cir.Center.Y;
                double radius = scale.yScale * cir.Radius;
                
                Point2d offset = new Point2d(x_offset, y_offset);
                if (radius > (calTarget.Radius * 2.0) || radius < (calTarget.Radius * .5) )
                {
                    Console.WriteLine("Cal Target Alignment Failed, Repeating Request. Radius: " + radius + " mm");
                    cameraToUse.RequestCircleLocation(roi, calTarget, targetZ);
                    return false;
                }
                else
                {
                    Console.WriteLine("Cal Circle Offset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                    // Use 1/2 the offset to calculate next location, we can't assume to know resolution, so we can't jump 
                    nxt.target.x = msg.target.x + (-1 * (offset.X/2) );
                    nxt.target.y = msg.target.y + (offset.Y/2);
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
