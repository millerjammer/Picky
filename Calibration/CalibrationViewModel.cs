using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using OpenCvSharp;

namespace Picky
{
    internal class CalibrationViewModel : INotifyPropertyChanged
    {
        private MachineModel machine;
        public CalTargetModel Target { get; set; } 
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        /* This is derived from above */
        public double DownCameraToPickHeadX
        {
            get { return machine.Cal.DownCameraToPickHeadX; }
            set { machine.Cal.DownCameraToPickHeadX = value; OnPropertyChanged(nameof(DownCameraToPickHeadX)); }
        }

        public double DownCameraToPickHeadY
        {
            get { return machine.Cal.DownCameraToPickHeadY; }
            set { machine.Cal.DownCameraToPickHeadY = value; OnPropertyChanged(nameof(DownCameraToPickHeadY)); }
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
            Target = new CalTargetModel();
        }

        public ICommand PerformCalibrationCommand { get { return new RelayCommand(PerformCalibration); } }
        private void PerformCalibration()
        {
            Target.PerformCalibration(machine);
        }

        public ICommand GoMonument00Command { get { return new RelayCommand(GoMonument00); } }
        private void GoMonument00()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Target.Grid00Location.X, Target.Grid00Location.Y, 0, 0, 0));
        }

        public ICommand GoMonument11Command { get { return new RelayCommand(GoMonument11); } }
        private void GoMonument11()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Target.Grid11Location.X, Target.Grid11Location.Y, 0, 0, 0));
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

        public ICommand MOToPHZ1 { get { return new RelayCommand(mOToPHZ1); } }
        private void mOToPHZ1()
        {
            machine.Cal.OriginToPickHeadX1 = machine.CurrentX;
            machine.Cal.OriginToPickHeadY1 = machine.CurrentY;
            machine.Cal.OriginToPickHeadZ1 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ);
        }

        public ICommand MOToPHZ2 { get { return new RelayCommand(mOToPHZ2); } }
        private void mOToPHZ2()
        {
            machine.Cal.OriginToPickHeadX2 = machine.CurrentX;
            machine.Cal.OriginToPickHeadY2 = machine.CurrentY;
            machine.Cal.OriginToPickHeadZ2 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ);
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

        /* This is called when machine z changes */
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ + Constants.TOOL_LENGTH_MM);
            machine.Cal.GetScaleMMPerPixAtZ(Machine.CurrentZ + Constants.TOOL_LENGTH_MM);
        }

        public ICommand GetResolutionAtPCBCommand { get { return new RelayCommand(GetResolutionAtPCB); } }
        private void GetResolutionAtPCB()
        {
           machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, true, Constants.FOCUS_PCB_062));
           GetScaleResolution(machine.Cal.TargetResAtPCB);
           machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, false, Constants.FOCUS_PCB_062));
        }

        public ICommand GetResolutionAtToolCommand { get { return new RelayCommand(GetResolutionAtTool); } }
        private void GetResolutionAtTool()
        {
            machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, true, Constants.FOCUS_TOOL_RETRIVAL));
            GetScaleResolution(machine.Cal.TargetResAtTool);
            machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, false, Constants.FOCUS_TOOL_RETRIVAL));
        }
              
        private void GetScaleResolution(CalResolutionTargetModel target)
        /*---------------------------------------------------------------------
         * Uses step alignment to determine mm/pix at a specific target. This
         * is a calibration used to enable jump step connections based on cammea
         * to target.  This function is performed at two different target z 
         * elevations.  This should be the first calibration that's done.
         * -------------------------------------------------------------------*/
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(target.targetCircle.Center.X, target.targetCircle.Center.Y, 0, 0, 0));
            for(int i = 0; i < 5; i++)
            {   
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(target.targetCircle, null));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            machine.Messages.Add(GCommand.G_ProbeZ(24.0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.SetScaleResolutionCalibration(machine, target));
            machine.Messages.Add(GCommand.G_SetPosition(target.targetCircle.Center.X, target.targetCircle.Center.Y, 0, 0, 0));
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
            machine.SaveSettings();
        }
    }
}
