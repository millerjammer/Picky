using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky.Commands
{
    public class SetToolTipLocationCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * This command uses the down camera to image the tip of the tool and calculate
     * it's position at various angles.  The result is placed in the tool's calibration
     * for the current angle and is used as an offset for each pick and place.
     * This command should run after every tool change. Write Pick Offset Cal Data
     * 
     * REQUIREMENTS:
     *      Head must be positioned at 0,0
     *      Up Illuminator 'Off'
     *      Down Illuminator 'On'
     *      Valid calibrations
     *-------------------------------------------------------------------------------*/

    {
        public MachineMessage msg;
        MachineModel machine;
        public PickToolModel tool;
        public RectangleDetector detector;
        CameraModel cameraToUse;

        private double last_x, last_y;


        public SetToolTipLocationCommand(PickToolModel _tool)
        {
            tool = _tool;
            machine = MachineModel.Instance;
            cameraToUse = machine.downCamera;
            detector = new RectangleDetector(machine.SelectedPickTool.CircleDetectorP1, machine.SelectedPickTool.CircleDetectorP2);
            detector.ROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), 0, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Tip Location\n");
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.IsManualFocus = true;
            cameraToUse.Focus = 600;
            cameraToUse.RequestRectangleLocation(detector);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsRectangleSearchActive() == false)
            {
                //Get offset in mm
                var scale = machine.Cal.GetScaleMMPerPixAtZ(25.0);
                OpenCvSharp.Rect bestRectangle = cameraToUse.GetBestRectangle();
                double x_offset = scale.xScale * (bestRectangle.Left + bestRectangle.Width/2);
                double y_offset = scale.yScale * (bestRectangle.Top + bestRectangle.Height);
                if (bestRectangle.Width > bestRectangle.Height)
                {
                    Console.WriteLine("Find Tool Tip Failed, Repeating Request. W > H");
                    cameraToUse.RequestRectangleLocation(detector);
                    return false;
                }
                else
                {
                    if (Math.Abs(last_x - x_offset) < 0.2 && Math.Abs(last_y - y_offset) < 0.2)
                    {
                        Console.WriteLine("PickOffset (mm): " + x_offset + " " + y_offset);
                        tool.SetPickOffsetCalibrationData(new Polar() { x = x_offset, y = y_offset, z = machine.CurrentZ });
                    }
                    else
                    {
                        last_x = x_offset;
                        last_y = y_offset;
                        cameraToUse.RequestRectangleLocation(detector);
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

}
