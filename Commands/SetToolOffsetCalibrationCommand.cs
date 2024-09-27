using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Media3D;

namespace Picky
{
    public class SetToolOffsetCalibrationCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * This command uses the up camera to image the tip of the tool and calculate
     * it's position at various angles.  The result is placed in the tool's calibration
     * for the current angle and is used as an offset for each pick and place.
     * This command should run after every tool change. Write Pick Offset Cal Data
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
        public CircleDetector detector;
        public CameraModel cameraToUse;
        public PickToolModel tool;
        

        public SetToolOffsetCalibrationCommand(MachineModel mm, PickToolModel _tool) 
        {
            machine = mm;
            tool = _tool;
            detector = new CircleDetector(HoughModes.GradientAlt, machine.Settings.tipSearchParam1, machine.Settings.tipSearchParam2, machine.Settings.tipSearchThreshold);
            detector.ROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), (Constants.CAMERA_FRAME_HEIGHT / 3), Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            detector.zEstimate = tool.Length;
            detector.CircleEstimate = new CircleSegment(new Point2f(0, 0), (float)(tool.SelectedTip.TipDia/2));
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Offset\n");
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
            if (tool.TipState != PickToolModel.TipStates.Ready)
                tool.TipState = PickToolModel.TipStates.Calibrating;
            cameraToUse.RequestCircleLocation(detector);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                //Get offset in mm
                var scale = machine.Cal.GetScaleMMPerPixAtZ(25.0);
                OpenCvSharp.CircleSegment bestCircle = cameraToUse.GetBestCircle();
                double x_offset = scale.xScale * bestCircle.Center.X;
                double y_offset = scale.yScale * bestCircle.Center.Y;
                double radius = scale.yScale * bestCircle.Radius;
                if (radius < 0.1)
                {
                    Console.WriteLine("PickOffset Failed, Repeating Request.");
                    cameraToUse.RequestCircleLocation(detector);
                    return false;
                }
                else
                {
                    Console.WriteLine("PickOffset (mm): " + x_offset + " " + y_offset + " radius: " + radius);
                    tool.SetPickOffsetCalibrationData(new Polar() { x = x_offset, y = y_offset, z = machine.CurrentZ });
                }
                return true;
            }
            return false;
        }
    }

    
}
