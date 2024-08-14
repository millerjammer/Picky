using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky
{
    public class SetToolOffsetCalibrationCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * This command uses the up camera to image the tip of the tool and calculate
     * it's position at various angles.  The result is placed in the tool's calibration
     * for the current angle and is used as an offset for each pick and place.
     * This command should run after every tool change.
     * 
     * REQUIREMENTS:
     *      Head must be positioned at 0,0
     *      Up Illuminator 'On'
     *      Down Illuminator 'Off'
     *      Valid calibrations
     *-------------------------------------------------------------------------------*/

    {
        public MachineModel machine;
        public MachineMessage msg;
        public OpenCvSharp.Rect roi;
        public CircleSegment tool;
        public CameraModel cameraToUse;


        public SetToolOffsetCalibrationCommand(MachineModel mm) 
        {
            machine = mm;
            roi = new OpenCvSharp.Rect(Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Offset\n");
            tool = new CircleSegment();
            tool.Center = new Point2f((float)machine.SelectedPickTool.ToolStorageX, (float)machine.SelectedPickTool.ToolStorageY);
            tool.Radius = ((float)(Constants.TOOL_28GA_TIP_DIA_MM / 2));
            cameraToUse = machine.upCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.IsManualFocus = true;
            cameraToUse.Focus = 600;
            cameraToUse.RequestCircleLocation(roi, tool, 25.0);
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
                    Console.WriteLine("PickOffset Failed, Repeating Request.");
                    cameraToUse.RequestCircleLocation(roi, tool, 25.0);
                    return false;
                }
                else
                {
                    Console.WriteLine("PickOffset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    //Write to calibration
                }
                return true;
            }
            return false;
        }
    }

    
}
