using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using OpenCvSharp;
using Picky.Tools;
using Xamarin.Forms.PlatformConfiguration.GTKSpecific;

namespace Picky
{
    public class SetToolCalibrationCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
     * Thsi command sets the tool's camera settings, captures the tool tip and 
     * calculates it's 3D position.  The tip of the tool must be placed on the
     * calibrated DeckPad or CalPad before issuing this command
     * 
     *-------------------------------------------------------------------------------*/

    {
        public MachineModel machine;
        public MachineMessage msg;

        public PickToolCalPosition CalPosition;
        private int delay;
       

        public SetToolCalibrationCommand(PickToolCalPosition calPosition)
        {
            machine = MachineModel.Instance;
            CalPosition = calPosition;

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Calibration\n");
            delay = (200 / Constants.QUEUE_SERVICE_INTERVAL);
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            machine.downCamera.Settings = CalPosition.CaptureSettings;
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (delay-- > 0)
                return false;

            // Save Current Z 
            CalPosition.TipPosition.Z = machine.CurrentZ;
            // Get 3D Position from Image
            CalPosition.Set3DToolTipFromToolMat(machine.downCamera.DilatedImage, machine.CurrentZ);
            // Save The Template Image 
            CalPosition.SaveToolTemplateImage();
            Application.Current.Dispatcher.Invoke(() =>
            {
                CalPosition.LoadToolTemplateImage();
            });
            return true;
        }
    }
}

