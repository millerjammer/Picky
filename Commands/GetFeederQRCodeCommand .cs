using OpenCvSharp;
using System;
using System.Text;
using System.Windows.Forms;

namespace Picky
{
    public class GetFeederQRCodeCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * This command uses the down camera to image a QR Code and calculate
     * it's position.  The Feeder data is updated 
     * 
     * REQUIREMENTS:
     *      Up Illuminator 'Off'
     *      Down Illuminator 'On'
     *      Valid calibrations
     *-------------------------------------------------------------------------------*/

    {
        public MachineModel machine;
        public MachineMessage msg;
        public CameraModel cameraToUse;
        public OpenCvSharp.Rect ROI;
        public Feeder currentFeeder;
        


        public GetFeederQRCodeCommand(Feeder feeder)
        {
            machine = MachineModel.Instance;
            currentFeeder = feeder;
            ROI = new OpenCvSharp.Rect(Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 4, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 2);
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Get Feeder QR Code\n");
            cameraToUse = machine.downCamera;
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            cameraToUse.RequestQRCodeLocation(ROI);
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (cameraToUse.IsQRSearchActive() == false)
            {
                //QR Points are already based on full frame, so get pixel offset from full frame
                double x_center_pix = (Constants.CAMERA_FRAME_WIDTH / 2) - ((cameraToUse.CurrentQRCodePoints[0].X + cameraToUse.CurrentQRCodePoints[2].X) / 2);
                double y_center_pix = (Constants.CAMERA_FRAME_HEIGHT / 2) - ((cameraToUse.CurrentQRCodePoints[0].Y + cameraToUse.CurrentQRCodePoints[2].Y) / 2);
                var scale = machine.Cal.GetScaleMMPerPixAtZ(Constants.ZOFFSET_CAL_PAD_TO_DECK + machine.Cal.CalPad.Z);
                double x_center_mm = msg.target.x + (scale.xScale * x_center_pix);
                double y_center_mm = msg.target.y - (scale.yScale * y_center_pix);
                if (currentFeeder != null)
                {
                    //Update Feeder QR Code and location
                    currentFeeder.Origin.X = x_center_mm;
                    currentFeeder.Origin.Y = y_center_mm;
                    currentFeeder.QRCode = cameraToUse.CurrentQRCode[0];
                }
                return true;
            }
            return false;
        }
    }


}

