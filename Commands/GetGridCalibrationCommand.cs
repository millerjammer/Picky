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
        private Position3D result;
        private OpenCvSharp.Rect roi;
        private Mat template;
        public CameraModel cameraToUse;
                                           
        public GetGridCalibrationCommand(Mat _template, OpenCvSharp.Rect _roi, Position3D _result)
        {
            machine = MachineModel.Instance;
            roi = _roi;
            template = _template;
            result = _result;
            
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
            cameraToUse.RequestTemplateSearch(template, roi);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsTemplateSearchActive() == false)
            {
                if (cameraToUse.GetTemplateMatches().Count == 9)
                {
                    double x_min = cameraToUse.GetTemplateMatches().OrderBy(p => p.X).Take(3).Average(p => p.X);
                    double y_min = cameraToUse.GetTemplateMatches().OrderBy(p => p.Y).Take(3).Average(p => p.Y);
                    double x_max = cameraToUse.GetTemplateMatches().OrderByDescending(p => p.X).Take(3).Average(p => p.X);
                    double y_max = cameraToUse.GetTemplateMatches().OrderByDescending(p => p.Y).Take(3).Average(p => p.Y);
                    if (result == null)
                        return true;
                    result.X = (2 * CalTargetModel.OPTICAL_GRID_X_MM) / (x_max - x_min);
                    result.Y = (2 * CalTargetModel.OPTICAL_GRID_Y_MM) / (y_max - y_min);
                    Console.WriteLine("MMPerPixel Calibration Complete. X: " + result.X + " mm/pix, Y: " + result.Y + " mm/pix");
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to find required matches (9).  Found: " + cameraToUse.GetTemplateMatches().Count);
                    return true;
                }
            }
            return false;
        }
    }
}

