using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Picky
{
    public class StepAlignToCalCircleCommand : MessageRelayCommand
    /*---------------------------------------------------------------
     * This function moves the head to a circle that looks like the
     * 'estimated' circle using iteration.  This is used when you don't 
     * know mm/pixel at the current Z.  This is typically used for 
     * calibration only.  
     * PREREQUISITS:
     *  - Resolution calibration must be set first.
     *  - Z Probe calibration must be set
     *  - Command must be followed by a position command.
     * -------------------------------------------------------------*/
    {
        public MachineModel machine;
        public CircleDetector detector;
        public CameraModel cameraToUse;
        public Position3D target;

        public int Param1 { get; set; } = 100;
        public double Param2 { get; set; } = 0.65;
        public int Threshold { get; set; } = 64;
        public double AdvanceFraction { get; set; } = 0.85;

        public StepAlignToCalCircleCommand(Position3D cal_target_to_update)
        {
            machine = MachineModel.Instance;
            target = cal_target_to_update;
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            MachineMessage msg = new MachineMessage();
            msg.target.x = target.X; msg.target.y = target.Y;
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Step Align To Calibration Circle\n");
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            // Build a detector to 5 times the radius of the target, config circle detector
            double offset_mm = (5 * target.Radius);
            var scale = machine.Cal.GetScaleMMPerPixAtZ(target.Z);
            double offset_pixels = (offset_mm / scale.xScale);
            double x_pixels = (Constants.CAMERA_FRAME_WIDTH / 2) - (offset_pixels / 2);
            double y_pixels = (Constants.CAMERA_FRAME_HEIGHT / 2) - (offset_pixels / 2);

            detector = new CircleDetector(HoughModes.GradientAlt, Param1, Param2, Threshold);
            detector.ROI = new OpenCvSharp.Rect((int)x_pixels, (int)y_pixels, (int)offset_pixels, (int)offset_pixels);
            detector.Radius = target.Radius;
            detector.zEstimate = target.Z;
            detector.IsManualFocus = true;
            detector.Focus = 0;
            detector.CountPerScene = 1;
            detector.ScenesToAquire = 50;                    // Number of Circles to find

            cameraToUse.RequestCircleLocation(detector);
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                //Take a guess of the offset, we're ultimately intersted in the machine position
                var scale = machine.Cal.GetScaleMMPerPixAtZ(target.Z);
                List<CircleSegment> cir = cameraToUse.GetBestCircles(); //In pixels
                double x_offset = scale.xScale * cir.Average(c => c.Center.X);
                double y_offset = scale.xScale * cir.Average(c => c.Center.Y);
                double radius = scale.xScale * cir.Average(c => c.Radius);

                Console.WriteLine("Cal Circle Offset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                // Use 1/2 the offset to calculate next location, we can't assume to know resolution, so we can't jump 
                nxt.target.x = machine.CurrentX - (float)(x_offset * AdvanceFraction);
                nxt.target.y = machine.CurrentY + (float)(y_offset * AdvanceFraction);
                nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                if (target != null)
                {
                    target.X = nxt.target.x;
                    target.Y = nxt.target.y;
                    target.IsValid = true;
                }

                return true;
            }
            return false;
        }
    }
}
