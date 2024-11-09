using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
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

        private double last_x, last_y;
        private int threshold;
        private int circleCount = 2;
        private int tolerance = 2;
        private OpenCvSharp.CircleSegment lastCircle;


        public SetToolOffsetCalibrationCommand(PickToolModel _tool, bool isUpper)
        {
            machine = MachineModel.Instance;
            tool = _tool;
            if (isUpper)
                threshold = machine.SelectedPickTool.MatUpperThreshold;
            else
                threshold = machine.SelectedPickTool.MatLowerThreshold;
            detector = new CircleDetector(HoughModes.GradientAlt, machine.SelectedPickTool.CircleDetectorP1, machine.SelectedPickTool.CircleDetectorP2, threshold);
            detector.ROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), 0, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 4);
            detector.zEstimate = tool.Length;
            detector.CircleEstimate = new CircleSegment(new Point2f(0, 0), (float)(tool.SelectedTip.TipDia / 2));

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Offset\n");
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            tool.TipState = PickToolModel.TipStates.Calibrating;
            cameraToUse.BinaryThreshold = threshold;
            cameraToUse.RequestCircleLocation(detector);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsCircleSearchActive() == false)
            {
                OpenCvSharp.CircleSegment circleSegment = cameraToUse.GetBestCircle();
                if (CircleCompare(lastCircle, circleSegment))
                {
                    if (--circleCount == 0)
                    {
                        tool.SetPickOffsetCalibrationData(new Polar() { x = circleSegment.Center.X, y = circleSegment.Center.Y, z = machine.CurrentZ });
                        return true;
                    }
                }
                else
                {
                    circleCount = 2;
                    lastCircle = circleSegment;
                    cameraToUse.RequestCircleLocation(detector);
                    return false;
                }
            }
            return false;
        }

        public bool CircleCompare(CircleSegment segment1, CircleSegment segment2)
        {
            bool centersEqual = Math.Abs(segment1.Center.X - segment2.Center.X) < tolerance &&
                        Math.Abs(segment1.Center.Y - segment2.Center.Y) < tolerance;

            return centersEqual;
        }
    }
}
