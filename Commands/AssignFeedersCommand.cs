using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Picky.Tools
{
    public class AssignFeedersCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * This command will capture QR codes, check to see if a feeder with that qr code
    * exists in the cassette.  If not, this command will search for it in the feeder
    * folder and add it to the cassette.  If if is not found in the folder it will 
    * create a new feeder and add it to the cassette. 
    *-------------------------------------------------------------------------------*/
    {
        public MachineModel machine;
        public MachineMessage msg;
        public Cassette cassette;
        int debounce;
                
        public AssignFeedersCommand(Cassette _cassette)
        {
            machine = MachineModel.Instance;
            cassette = _cassette;
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Assign Feeders\n");
            debounce = 0;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            machine.downCamera.RequestQRDecode = true;
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            var qrZoneResults = machine.downCamera.GetQrZoneResults();
            var scale = machine.Cal.GetScaleMMPerPixAtZ(machine.Cal.QRRegion.Z);
            /* Update Feeders */
            foreach (var feeder in machine.SelectedCassette.Feeders)
            {
                var match = qrZoneResults.FirstOrDefault(qr => qr.str == feeder.QRCode);
                if (!match.Equals(default((string, OpenCvSharp.Rect))))
                {
                    double x_offset_pix = (match.pos.X + (match.pos.Width / 2)) - (Constants.CAMERA_FRAME_WIDTH / 2);
                    double x_offset_mm = scale.xScale * x_offset_pix;
                    double x = machine.Current.X - x_offset_mm;

                    double y_offset_pix = (Constants.CAMERA_FRAME_HEIGHT / 2) - (match.pos.Y + (match.pos.Height / 2));
                    double y_offset_mm = scale.yScale * y_offset_pix;
                    double y = machine.Current.Y - y_offset_mm;

                    if (x < Constants.TRAVEL_LIMIT_X_MM && y < Constants.TRAVEL_LIMIT_Y_MM)
                    {   /* Only update feeder if we can get to it */
                        feeder.Origin.X = x;
                        feeder.Origin.Y = y;
                    }
                    qrZoneResults.Remove(match);
                }
            }
            /* Load or Create new Feeders from remaining qr codes */
            App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var qr in qrZoneResults)
                    {
                        if (!string.IsNullOrEmpty(qr.str))
                        {
                            double x_offset_pix = (qr.pos.X + (qr.pos.Width / 2)) - (Constants.CAMERA_FRAME_WIDTH / 2);
                            double x_offset_mm = scale.xScale * x_offset_pix;
                            double x = machine.Current.X - x_offset_mm;

                            double y_offset_pix = (Constants.CAMERA_FRAME_HEIGHT / 2) - (qr.pos.Y + (qr.pos.Height / 2));
                            double y_offset_mm = scale.yScale * y_offset_pix;
                            double y = machine.Current.Y - y_offset_mm;
                            if (x < Constants.TRAVEL_LIMIT_X_MM && y < Constants.TRAVEL_LIMIT_Y_MM)
                            {   /* Only update feeder if we can get to it */
                                FeederModel feeder = FileUtils.LoadFeederFromQRCode(qr.str);
                                feeder.QRCode = qr.str;
                                machine.SelectedCassette.Feeders.Add(feeder);
                            }
                        }
                    }
                });
            if (machine.IsMachineInMotion == false && ++debounce > 3)
            {
                machine.downCamera.RequestQRDecode = false;
                return true;
            }

            return false;
        }
    }
}