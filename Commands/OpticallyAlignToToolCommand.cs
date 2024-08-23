using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class OpticallyAlignToToolCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * This command uses the down camera to image the top of the tool and OVERWRITES 
    * the position of the next command to add an offset to align head.  This command
    * should run after every tool change.
    * 
    * REQUIREMENTS:
    *      Up Illuminator 'Off'
    *      Down Illuminator 'On'
    *      Valid calibrations
    *-------------------------------------------------------------------------------*/
    {

        public MachineModel machine;
        public MachineMessage msg;
        public PickToolModel tool;
        public CircleDetector detector;
        public CameraModel cameraToUse;

        public OpticallyAlignToToolCommand(PickToolModel pickTool)
        {
            machine = MachineModel.Instance;
            tool = pickTool;
            detector = new CircleDetector(HoughModes.Gradient, 140, 50);
            detector.ROI = new OpenCvSharp.Rect((2 * Constants.CAMERA_FRAME_WIDTH) / 5, (2 * Constants.CAMERA_FRAME_HEIGHT) / 5, Constants.CAMERA_FRAME_WIDTH / 5, Constants.CAMERA_FRAME_HEIGHT / 5);
            detector.zEstimate = 35.5;
            detector.CircleEstimate = new CircleSegment(new Point2f(0, 0), ((float)(Constants.TOOL_CENTER_RADIUS_MILS * Constants.MIL_TO_MM)));
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Optically Align to Tool Head\n");
            cameraToUse = machine.downCamera;
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
                //Get offset in mm
                var scale = machine.Cal.GetScaleMMPerPixAtZ(45.56);
                OpenCvSharp.CircleSegment bestCircle = cameraToUse.GetBestCircle();
                double x_offset = scale.xScale * bestCircle.Center.X;
                double y_offset = scale.yScale * bestCircle.Center.Y;
                double radius = scale.yScale * bestCircle.Radius;
                if (radius < 0.1)
                {
                    Console.WriteLine("Optically Align to Tool Failed, Repeating Request.");
                    cameraToUse.RequestCircleLocation(detector);
                    return false;
                }
                else
                {
                    Console.WriteLine("Tool Offset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                    nxt.target.x = tool.ToolStorageX - x_offset;
                    nxt.target.y = tool.ToolStorageY + y_offset;
                    nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                    //Save to tool
                    tool.ToolReturnLocation = new Point2d(nxt.target.x, nxt.target.y);
                }
                return true;
            }
            return false;
        }
    }
}