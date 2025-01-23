using EnvDTE90;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Picky
{
    public class StepAlignToTemplateCommand : MessageRelayCommand
    /*---------------------------------------------------------------
     * This function moves the head to a circle that looks like the
     * 'estimated' circle using iteration.  This is used when you don't 
     * know mm/pixel at the current Z.  This is typically used for 
     * calibration only.  
     * PREREQUISITS:
     *  - Resolution calibration must be set first.
     *  - Z Probe calibration must be set
     *  - Command must be followed by a position command.
     * -------------------------------------------------------------*/
    {
        public MachineModel machine;
        public CameraModel cameraToUse;
        public MachineMessage msg;
        public Position3D result;
        public Mat template;
        OpenCvSharp.Rect roi;
        public double xScale, yScale;
        public double AdvanceFraction { get; set; } = 0.85;

        public StepAlignToTemplateCommand(Mat _template, OpenCvSharp.Rect _roi, Position3D _result)
        {
            machine = MachineModel.Instance;
            cameraToUse = machine.downCamera;
            template = _template;
            roi = _roi;
            result = _result;
        }

        public MachineMessage GetMessage()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Step Align To Template\n");
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
                List<Position3D> matches = cameraToUse.GetTemplateMatches(); 
                // In pixels, bbsolute position in frame
                double x_offset = matches.ElementAt(0).X + (matches.ElementAt(0).Width / 2);
                double y_offset = matches.ElementAt(0).Y + (matches.ElementAt(0).Height / 2);
                // In pixels, offset from center of image
                x_offset -= Constants.CAMERA_FRAME_WIDTH / 2;
                y_offset -= Constants.CAMERA_FRAME_HEIGHT / 2;
                // In mm - USING DEFAULT - This function is intended to find mm/pix or mm/step, thus assumptions made
                x_offset *= Constants.DEFAULT_MM_PER_PIXEL;
                y_offset *= Constants.DEFAULT_MM_PER_PIXEL;

                Console.WriteLine("Template Offset (mm): " + x_offset + " " + y_offset + " offset: " + matches.ElementAt(0).ToString());
                MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                nxt.target.x = machine.Current.X - (float)(x_offset * AdvanceFraction);
                nxt.target.y = machine.Current.Y + (float)(y_offset * AdvanceFraction);
                nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                if (result != null)
                {
                    result.X = machine.Current.X;
                    result.Y = machine.Current.Y;
                }
                return true;
            }
            Console.WriteLine("no dice");
            return false;
        }
    }
}
