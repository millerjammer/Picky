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
        private MachineModel machine;
        //public CalTargetModel Target { get; set; } 
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }
        
        public CalTargetModel Target
        {
            get { return machine.Cal.CalTarget; }
            set { machine.Cal.CalTarget = value; OnPropertyChanged(nameof(Target)); }
        }

        /* This is derived from above */
        public double DownCameraToPickHeadX
        {
            get { return machine.Cal.PickHeadToCameraX; }
            set { machine.Cal.PickHeadToCameraX = value; OnPropertyChanged(nameof(DownCameraToPickHeadX)); }
        }

        public double DownCameraToPickHeadY
        {
            get { return machine.Cal.PickHeadToCameraY; }
            set { machine.Cal.PickHeadToCameraY = value; OnPropertyChanged(nameof(DownCameraToPickHeadY)); }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CalibrationViewModel(MachineModel mm)
        {
            machine = mm;
            machine.PropertyChanged += OnMachinePropertyChanged;
            //Target = new CalTargetModel();
        }

        public ICommand CalibrateMMPerStepCommand { get { return new RelayCommand(CalibrateMMPerStep); } }
        private void CalibrateMMPerStep()
        {
             Target.CalibrateMMPerStep();
        }

        
        public ICommand SetMonument00Command { get { return new RelayCommand(SetMonument00); } }
        private void SetMonument00()
        {
            Target.ActualLoc00.X = machine.CurrentX;
            Target.ActualLoc00.Y = machine.CurrentY;
        }

        public ICommand GoMonument00Command { get { return new RelayCommand(GoMonument00); } }
        private void GoMonument00()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Target.ActualLoc00.X, Target.ActualLoc00.Y, 0, 0, 0));
        }

        public ICommand SetMonument11Command { get { return new RelayCommand(SetMonument11); } }
        private void SetMonument11()
        {
            Target.ActualLoc11.X = machine.CurrentX;
            Target.ActualLoc11.Y = machine.CurrentY;
        }

        public ICommand GoMonument11Command { get { return new RelayCommand(GoMonument11); } }
        private void GoMonument11()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Target.ActualLoc11.X, Target.ActualLoc11.Y, 0, 0, 0));
        }
            
        public ICommand WriteStepPerUnitCommand { get { return new RelayCommand(WriteStepPerUnit); } }
        private void WriteStepPerUnit()
        {
            machine.Messages.Add(GCommand.G_SetStepsPerUnit(machine.Cal.CalculatedStepsPerUnitX, machine.Cal.CalculatedStepsPerUnitY));
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
        }

        public ICommand ReadStepPerUnitCommand { get { return new RelayCommand(ReadStepPerUnit); } }
        private void ReadStepPerUnit()
        {
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
        }

        public ICommand OffsetCameraToHeadCommand { get { return new RelayCommand(OffsetToHead); } }
        private void OffsetToHead()
        {
            var offset = machine.Cal.GetPickHeadOffsetToCameraAtZ(machine.CurrentZ);
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.CurrentX - offset.x_offset, machine.CurrentY - offset.y_offset));
        }

        public ICommand OffsetHeadToCameraCommand { get { return new RelayCommand(OffsetToCamera); } }
        private void OffsetToCamera()
        {
            var offset = machine.Cal.GetPickHeadOffsetToCameraAtZ(machine.CurrentZ);
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.CurrentX + offset.x_offset, machine.CurrentY + offset.y_offset));
        }

        public ICommand SetOriginToUpCameraCommand { get { return new RelayCommand(SetOriginToUpCamera); } }
        private void SetOriginToUpCamera()
        {
            machine.Cal.OriginToUpCameraX = machine.CurrentX;
            machine.Cal.OriginToUpCameraY = machine.CurrentY;
        }

        public ICommand GoToUpCameraCommand { get { return new RelayCommand(GoToUpCamera); } }
        private void GoToUpCamera()
        {
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.Cal.OriginToUpCameraX, machine.Cal.OriginToUpCameraY));
        }

        public ICommand SetOriginToDownCameraCommand { get { return new RelayCommand(SetOriginToDownCamera); } }
        private void SetOriginToDownCamera()
        {
            machine.Cal.OriginToDownCameraX = machine.CurrentX;
            machine.Cal.OriginToDownCameraY = machine.CurrentY;
        }

        public ICommand GoToDownCameraCommand { get { return new RelayCommand(GoToDownCamera); } }
        private void GoToDownCamera()
        {
            machine.Messages.Add(GCommand.G_SetXYPosition(machine.Cal.OriginToDownCameraX, machine.Cal.OriginToDownCameraY));
        }

        public ICommand GetFeeder0Command { get { return new RelayCommand(GetFeeder0); } }
        private void GetFeeder0()
        {
            machine.Cal.Feeder0X = machine.CurrentX;
            machine.Cal.Feeder0Y = machine.CurrentY;
        }

        public ICommand GetFeederNCommand { get { return new RelayCommand(GetFeederN); } }
        private void GetFeederN()
        {
            machine.Cal.FeederNX = machine.CurrentX;
            machine.Cal.FeederNY = machine.CurrentY;
        }

        public ICommand GoToFeederNCommand { get { return new RelayCommand(GoToFeederN); } }
        private void GoToFeederN()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.FeederNX, machine.Cal.FeederNY - 40, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.FeederNX, machine.Cal.FeederNY, 0, 0, 0));
        }
        public ICommand GoToFeeder0Command { get { return new RelayCommand(GoToFeeder0); } }
        private void GoToFeeder0()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Feeder0X, machine.Cal.Feeder0Y - 40, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Feeder0X, machine.Cal.Feeder0Y, 0, 0, 0));
        }
        /*-----------------*/

        public ICommand SetUpperTargetResCommand { get { return new RelayCommand(SetUpperTargetRes); } }
        private void SetUpperTargetRes()
        {
            machine.Cal.IsPreviewLowerTargetActive = false;
            machine.Cal.IsPreviewUpperTargetActive = false;
            Target.SetUpperCalTarget();
        }

        public ICommand SetLowerTargetResCommand { get { return new RelayCommand(SetLowerTargetRes); } }
        private void SetLowerTargetRes()
        {
            machine.Cal.IsPreviewLowerTargetActive = false;
            machine.Cal.IsPreviewUpperTargetActive = false;
            Target.SetLowerCalTarget();
        }
        
        /* This is called when machine z changes */
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ);
            //machine.Cal.GetScaleMMPerPixAtZ(Machine.CurrentZ + Constants.TOOL_LENGTH_MM);
        }
               
        public ICommand CalZProbeCommand { get { return new RelayCommand(CalZProbe); } }
        private void CalZProbe()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.ZCalPadX, machine.Cal.ZCalPadY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.GetZProbe(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
        }

        public ICommand GoToDeckPadCommand { get { return new RelayCommand(GoToDeckPad); } }
        private void GoToDeckPad()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.ZCalDeckPadX, machine.Cal.ZCalDeckPadY, 0, 0, 0));
        }

        public ICommand GoToCalibrationPadCommand { get { return new RelayCommand(GoToCalibrationPad); } }
        private void GoToCalibrationPad()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.ZCalPadX, machine.Cal.ZCalPadY, 0, 0, 0));
        }

        public ICommand CalibrateMMPerPixCommand { get { return new RelayCommand(CalibrateMMPerPix); } }
        private void CalibrateMMPerPix()
        {
            machine.Cal.IsPreviewLowerTargetActive = false;
            machine.Cal.IsPreviewUpperTargetActive = false;
            Target.CalibrateMMPerPixelAtZ();
        }
        
        public ICommand MoveToPickLocation { get { return new RelayCommand(moveToPickLocation); } }
        private void moveToPickLocation()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            machine.Messages.Add(GCommand.G_SetPosition(dest.x + DownCameraToPickHeadX, dest.y + DownCameraToPickHeadY, 0, 0, 0));
        }

        public ICommand MoveToCameraLocation { get { return new RelayCommand(moveToCameraLocation); } }
        private void moveToCameraLocation()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            machine.Messages.Add(GCommand.G_SetPosition(dest.x - DownCameraToPickHeadX, dest.y - DownCameraToPickHeadY, 0, 0, 0));
        }

        public ICommand OkCommand { get { return new RelayCommand(okCommand); } }
        private void okCommand()
        {
            machine.SaveCalibration();
        }
    }
}
