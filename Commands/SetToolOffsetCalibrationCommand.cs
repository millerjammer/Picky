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
using Xamarin.Forms;

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
        private int focus;
        private int circleCount = 2;
        private int tolerance = 2;
        private OpenCvSharp.CircleSegment lastCircle;


        public SetToolOffsetCalibrationCommand(PickToolModel _tool, bool isUpper)
        {
            machine = MachineModel.Instance;
            tool = _tool;
            if (isUpper)
            {
                focus = machine.SelectedPickTool.UpperCircleDetector.Focus;
                threshold = machine.SelectedPickTool.UpperCircleDetector.Threshold;
                detector = new CircleDetector(HoughModes.GradientAlt, machine.SelectedPickTool.UpperCircleDetector.Param1, machine.SelectedPickTool.UpperCircleDetector.Param2, threshold);
            }
            else
            {
                focus = machine.SelectedPickTool.LowerCircleDetector.Focus;
                threshold = machine.SelectedPickTool.LowerCircleDetector.Threshold;
                detector = new CircleDetector(HoughModes.GradientAlt, machine.SelectedPickTool.LowerCircleDetector.Param1, machine.SelectedPickTool.LowerCircleDetector.Param2, threshold);
            }
            
            detector.ROI = tool.SearchToolROI;
            detector.zEstimate = tool.Length;
            detector.Radius = (tool.SelectedTip.TipDia / 2);

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
            cameraToUse.Focus = focus;
            cameraToUse.IsManualFocus = true;
            cameraToUse.CircleDetectorP1 = detector.Param1;
            cameraToUse.CircleDetectorP2 = detector.Param2;
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
                        Position3D pos = new Position3D()
                        {
                            X = circleSegment.Center.X,
                            Y = circleSegment.Center.Y,
                            Z = (machine.CurrentZ + tool.Length),
                            Angle = machine.CurrentA
                        };
                        tool.SetPickOffsetCalibrationData( pos );
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
