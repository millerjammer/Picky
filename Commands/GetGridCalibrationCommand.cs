using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Picky.Commands
{
    internal class GetGridCalibrationCommand : MessageRelayCommand
    {

        public MachineModel machine;
        public MachineMessage msg;
        public CircleDetector detector;
        public CameraModel cameraToUse;
        List<CircleSegment> bestCircles;

        private int sceneCount = 6;
       
        public GetGridCalibrationCommand(List<CircleSegment> bestC, Position3D centerCircle)
        {
            machine = MachineModel.Instance;
            bestCircles = bestC;
            
            double grid_dist_mm = CalTargetModel.OPTICAL_GRID_X_MM;
            var scale = machine.Cal.GetScaleMMPerPixAtZ(centerCircle.Z);
            int x_pix = (int)((Constants.CAMERA_FRAME_WIDTH / 2) - ((1.5 * grid_dist_mm) / scale.xScale));
            int y_pix = (int)((Constants.CAMERA_FRAME_HEIGHT / 2) - ((1.5 * grid_dist_mm) / scale.yScale));
            int w_pix = (int)((3 * grid_dist_mm) / scale.xScale);
            int h_pix = (int)((3 * grid_dist_mm) / scale.yScale);

            detector = new CircleDetector(HoughModes.GradientAlt, 100, 0.65, 64);
            detector.IsManualFocus = false;
            detector.ROI = new Rect(x_pix, y_pix, w_pix, h_pix);
            detector.zEstimate = centerCircle.Z;
            detector.Radius = CalTargetModel.OPTICAL_GRID_RADIUS_MM;
            detector.ScenesToAquire = 1;
            detector.CountPerScene = 9;

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Get Grid Calibration\n");
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
                OpenCvSharp.CircleSegment circleSegment = cameraToUse.GetBestCircle();
                if (--sceneCount <= 0) { 
                    bestCircles = cameraToUse.GetBestCircles();
                    return true;
                }
                else
                {
                    cameraToUse.RequestCircleLocation(detector);
                    return false;
                }
            }
            return false;
        }
    }
}

