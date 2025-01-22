using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Picky
{
    internal class CalibrationViewModel : INotifyPropertyChanged
    {
        public MachineModel machine { get; set; }
                                
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CalibrationViewModel()
        {
            machine = MachineModel.Instance;
        }
                                  
        public ICommand WriteStepPerUnitCommand { get { return new RelayCommand(WriteStepPerUnit); } }
        private void WriteStepPerUnit()
        {
            machine.Messages.Add(GCommand.G_SetStepsPerUnit(machine.Cal.CalculatedStepsPerUnitX, machine.Cal.CalculatedStepsPerUnitY, machine.Cal.CalculatedStepsPerUnitZ));
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
        }

        public ICommand ReadStepPerUnitCommand { get { return new RelayCommand(ReadStepPerUnit); } }
        private void ReadStepPerUnit()
        {
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
        }

        public ICommand SetOriginCasetteQRCommand { get { return new RelayCommand(SetOriginCasetteQR); } }
        private void SetOriginCasetteQR()
        {
            // Set the Region AROUND this Y- Dimension ONLY
            double y = machine.Current.Y;
            double z = machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_QR;
            machine.Cal.QRRegion = new Position3D(Constants.TRAVEL_LIMIT_X_MM, y, z, Constants.TRAVEL_LIMIT_X_MM, Constants.QR_CODE_SIZE_MM);
            machine.Cal.QRCaptureSettings = machine.downCamera.Settings.Clone();
        }

        public ICommand PreviewOriginCassetteQRCommand { get { return new RelayCommand(PreviewOriginCassetteQR); } }
        private void PreviewOriginCassetteQR()
        {
            machine.downCamera.Settings = machine.Cal.QRCaptureSettings.Clone();
            // This is a ROI and thus a camera term offset to center origin.
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.Cal.QRRegion.X, machine.Cal.QRRegion.Y));
            machine.Messages.Add(GCommand.SetCamera(machine.Cal.QRCaptureSettings, machine.downCamera));
            
        }

        public ICommand SetOriginPartChannelCommand { get { return new RelayCommand(SetOriginPartChannel); } }
        private void SetOriginPartChannel()
        {
            // Set the Region AROUND this Y- Dimension ONLY
            double y = machine.Current.Y;
            double z = machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE;
            machine.Cal.ChannelRegion = new Position3D(Constants.TRAVEL_LIMIT_X_MM, y, z, Constants.TRAVEL_LIMIT_X_MM, Constants.TRAVEL_LIMIT_Y_MM - y);
            machine.Cal.ChannelCaptureSettings = machine.downCamera.Settings.Clone();
        }

        public ICommand PreviewOriginPartChannelCommand { get { return new RelayCommand(PreviewOriginPartChannel); } }
        private void PreviewOriginPartChannel()
        {
            machine.downCamera.Settings = machine.Cal.ChannelCaptureSettings.Clone();
            // This is a ROI and thus a camera term offset to upper left origin.
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.Cal.ChannelRegion.X, machine.Cal.ChannelRegion.Y));
            machine.Messages.Add(GCommand.SetCamera(machine.Cal.ChannelCaptureSettings, machine.downCamera));
        }

        /*-----------------*/
                
               
        public ICommand CalZProbeCommand { get { return new RelayCommand(CalZProbe); } }
        private void CalZProbe()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X + Constants.CAMERA_TO_HEAD_OFFSET_X_MM, machine.Cal.CalPad.Y + Constants.CAMERA_TO_HEAD_OFFSET_Y_MM, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.GetZProbe(machine.Cal.CalPad));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
        }

        public ICommand SetStepOriginCommand { get { return new RelayCommand(SetStepOrigin); } }
        private void SetStepOrigin()
        {
            machine.Cal.Target.StepPad.X = machine.Current.X; machine.Cal.Target.StepPad.Y = machine.Current.Y;
        }

        public ICommand GoToStepOriginCommand { get { return new RelayCommand(GoToStepOrigin); } }
        private void GoToStepOrigin()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Target.StepPad.X, machine.Cal.Target.StepPad.Y, 0, 0, 0));
        }

        public ICommand GoToDeckPadCommand { get { return new RelayCommand(GoToDeckPad); } }
        private void GoToDeckPad()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.DeckPad.X, machine.Cal.DeckPad.Y, 0, 0, 0));
        }

        public ICommand GoToCalibrationPadCommand { get { return new RelayCommand(GoToCalibrationPad); } }
        private void GoToCalibrationPad()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X, machine.Cal.CalPad.Y, 0, 0, 0));
        }
        
        public ICommand SetCurrentDriveLineCommand { get { return new RelayCommand(SetCurrentDriveLine); } }
        private void SetCurrentDriveLine()
        {
            machine.Cal.DriveLineY = machine.Current.Y;
        }

        public ICommand GoToMachineDriveLineCommand { get { return new RelayCommand(GoToMachineDriveLine); } }
        private void GoToMachineDriveLine()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Current.X, machine.Cal.DriveLineY, 0, 0, 0));
        }

        public ICommand CalibrateMMPerPixCommand { get { return new RelayCommand(CalibrateMMPerPix); } }
        private void CalibrateMMPerPix()
        {
            machine.Cal.IsPreviewLowerTargetActive = false;
            machine.Cal.IsPreviewUpperTargetActive = false;
            machine.Cal.IsPreviewGridActive = false;
            machine.Cal.Target.CalibrateMMPerPixelAtZ();
        }

        public ICommand TestMMPerPixCommand { get { return new RelayCommand(TestMMPerPix); } }
        private void TestMMPerPix()
        {
            machine.Cal.Target.TestMMPerPixelAtZ();
        }

        public ICommand CalibrateMMPerStepCommand { get { return new RelayCommand(CalibrateMMPerStep); } }
        private void CalibrateMMPerStep()
        {
            machine.Cal.IsPreviewLowerTargetActive = false;
            machine.Cal.IsPreviewUpperTargetActive = false;
            machine.Cal.IsPreviewGridActive = false;
            machine.Cal.Target.CalibrateMMPerStep();
        }
               

        public ICommand OkCommand { get { return new RelayCommand(okCommand); } }
        private void okCommand()
        {
            machine.SaveCalibration();
        }
    }
}
