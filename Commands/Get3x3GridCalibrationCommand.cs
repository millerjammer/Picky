using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using static Picky.MachineMessage;

namespace Picky.Commands
{
    internal class Get3x3GridCalibrationCommand : MessageRelayCommand
    {

        public MachineModel machine;
        public MachineMessage msg;
        private Position3D result;
        private OpenCvSharp.Rect roi;
        private Mat template;
        public CameraModel cameraToUse;
                                           
        public Get3x3GridCalibrationCommand(Mat _template, OpenCvSharp.Rect _roi, Position3D _result)
        {
            machine = MachineModel.Instance;
            roi = _roi;
            template = _template;
            result = _result;
            
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Get GridOrigin Calibration\n");
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
                    /* Use only Vertical and Horizontal Centers to avoid pin-cushion error */
                    Position3D p1 = cameraToUse.GetTemplateMatches().OrderBy(p => p.X).Take(3).OrderBy(p => p.Y).ElementAt(1);
                    double x_min = p1.X;
                    Position3D p2 = cameraToUse.GetTemplateMatches().OrderByDescending(p => p.X).Take(3).OrderBy(p => p.Y).ElementAt(1);
                    double x_max = p2.X;
                    Position3D p3 = cameraToUse.GetTemplateMatches().OrderBy(p => p.Y).Take(3).OrderBy(p => p.X).ElementAt(1);
                    double y_min = p3.Y;
                    Position3D p4 = cameraToUse.GetTemplateMatches().OrderByDescending(p => p.Y).Take(3).OrderBy(p => p.X).ElementAt(1);
                    double y_max = p4.Y;

                    Console.WriteLine("p1: " + p1.ToString());
                    Console.WriteLine("p2: " + p2.ToString());
                    Console.WriteLine("p3: " + p3.ToString());
                    Console.WriteLine("p4: " + p4.ToString());

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

