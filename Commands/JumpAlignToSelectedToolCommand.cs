using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class JumpAlignToSelectedToolCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * This command uses the down camera to image the top of the tool and update the 
    * position of the next command to add an offset to align head.  This command
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
        public OpenCvSharp.Rect roi;
        public CircleSegment tool;
        public CameraModel cameraToUse;

        public JumpAlignToSelectedToolCommand(MachineModel mm)
        {
            machine = mm;
            roi = new OpenCvSharp.Rect((2 * Constants.CAMERA_FRAME_WIDTH) / 5, (2 * Constants.CAMERA_FRAME_HEIGHT) / 5, Constants.CAMERA_FRAME_WIDTH / 5, Constants.CAMERA_FRAME_HEIGHT / 5);
            tool = new CircleSegment();
            tool.Center = new Point2f((float)machine.SelectedPickTool.ToolStorageX, (float)machine.SelectedPickTool.ToolStorageY);
            tool.Radius = ((float)(Constants.TOOL_CENTER_RADIUS_MILS * Constants.MIL_TO_MM));
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Jump Align\n");
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            //Use the calibrated z for height
            cameraToUse.RequestCircleLocation(roi, tool, machine.Cal.TargetResAtTool.MMHeightZ);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                //Get offset in mm
                var scale = machine.Cal.GetScaleMMPerPixAtZ(35.56);
                OpenCvSharp.CircleSegment bestCircle = cameraToUse.GetBestCircle();
                double x_offset = scale.xScale * bestCircle.Center.X;
                double y_offset = scale.yScale * bestCircle.Center.Y;
                double radius = scale.yScale * bestCircle.Radius;
                if (radius < 0.1)
                {
                    Console.WriteLine("Optically Align to Tool Failed, Repeating Request.");
                    cameraToUse.RequestCircleLocation(roi, tool, machine.Cal.TargetResAtTool.MMHeightZ);
                    return false;
                }
                else
                {
                    Console.WriteLine("Tool Offset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                    nxt.target.x += x_offset;
                    nxt.target.y += y_offset;
                }
                return true;
            }
            return false;
        }
    }
}
