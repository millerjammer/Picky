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
    public class GetTemplatePositionCommand : MessageRelayCommand
    /*---------------------------------------------------------------
     * Given a template and a z this will populate the position in 
     * mm.  The roi MUST include z. (x, y, width, height in pixels, z in mm)
     * A position X,Y command must follow
     * -------------------------------------------------------------*/
    {
        public MachineModel machine;
        public CameraModel cameraToUse;
        public MachineMessage msg;
        public Position3D roi;
        public Mat template;
                
        public GetTemplatePositionCommand(Mat _template, Position3D _roi)
        {
            machine = MachineModel.Instance;
            cameraToUse = machine.downCamera;
            template = _template;
            roi = _roi;
        }

        public MachineMessage GetMessage()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Get Template Position\n");
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.RequestTemplateSearch(template, roi.GetRect());
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsTemplateSearchActive() == false)
            {
                List<Position3D> matches = cameraToUse.GetTemplateMatches();
                // In pixels, absolute position in frame
                Position3D target = matches.OrderBy(pos => (pos.X * pos.X) + (pos.Y * pos.Y)).FirstOrDefault();
                Console.WriteLine("target: " + target.ToString());
                Position3D target_mm = TranslationUtils.ConvertFrameRectPosPixToGlobalMM(target, roi.Z);
                Console.WriteLine("target_mm: " + target_mm.ToString());
                MachineMessage nxt = machine.Messages.ElementAt(machine.Messages.IndexOf(msg) + 1);
                nxt.target.x = target_mm.X;
                nxt.target.y = target_mm.Y;
                nxt.cmd = Encoding.UTF8.GetBytes(string.Format("G0 X{0} Y{1}\n", nxt.target.x, nxt.target.y));
                return true;
            }
            return false;
        }
    }
}

