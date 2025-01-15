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
        private long delay = 1000;
        private long start_ms;
       

        public SetToolCalibrationCommand(PickToolCalPosition calPosition)
        {
            machine = MachineModel.Instance;
            CalPosition = calPosition;

            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Set Tool Calibration\n");
 
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            start_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            machine.downCamera.Settings = CalPosition.CaptureSettings.Clone();
            return true;
        }


        public bool PostMessageCommand(MachineMessage msg)
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < (start_ms + delay))
                return false;

            // Save Tool Length when calibrating @ the Calibrated Cal Pad 
            double offset_to_head_x = Constants.CAMERA_TO_HEAD_OFFSET_X_MM;
            double offset_to_head_y = Constants.CAMERA_TO_HEAD_OFFSET_Y_MM;
            if (machine.Current.X == (machine.Cal.CalPad.X + offset_to_head_x) && machine.Current.Y == (machine.Cal.CalPad.Y + offset_to_head_y))
            {
                machine.SelectedPickTool.Length = machine.Cal.CalPad.Z - machine.Current.Z;
                Console.WriteLine("Captured Length: " + machine.SelectedPickTool.Length);
            }
            // Save Current Z 
            CalPosition.TipPosition.Z = (machine.Current.Z + machine.SelectedPickTool.Length);
            
            // Get 3D Position from Image
            CalPosition.Set3DToolTipFromToolMat(machine.downCamera.DilatedImage, CalPosition.TipPosition.Z);
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

