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

         public double StepsPerUnitX
        {
            get { return machine.Cal.StepsPerUnitX; }
            set { machine.Cal.StepsPerUnitX = value; OnPropertyChanged(nameof(StepsPerUnitX)); }
        }

        public double StepsPerUnitY
        {
            get { return machine.Cal.StepsPerUnitY; }
            set { machine.Cal.StepsPerUnitY = value; OnPropertyChanged(nameof(StepsPerUnitY)); }
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
            machine.Messages.Add(GCommand.G_SetStepsPerUnit(machine.Cal.StepsPerUnitX, machine.Cal.StepsPerUnitY));
        }

        public ICommand ReadStepPerUnitCommand { get { return new RelayCommand(ReadStepPerUnit); } }
        private void ReadStepPerUnit()
        {
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
        }

        public ICommand MOToPHZ1 { get { return new RelayCommand(mOToPHZ1); } }
        private void mOToPHZ1()
        {
            machine.Cal.MachineOriginToPickHeadX1 = machine.CurrentX;
            machine.Cal.MachineOriginToPickHeadY1 = machine.CurrentY;
            machine.Cal.MachineOriginToPickHeadZ1 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ);
        }

        public ICommand MOToPHZ2 { get { return new RelayCommand(mOToPHZ2); } }
        private void mOToPHZ2()
        {
            machine.Cal.MachineOriginToPickHeadX2 = machine.CurrentX;
            machine.Cal.MachineOriginToPickHeadY2 = machine.CurrentY;
            machine.Cal.MachineOriginToPickHeadZ2 = machine.CurrentZ;
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
            machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.CurrentZ);
            machine.Cal.GetScaleMMPerPixAtZ(Machine.CurrentZ);
        }

        public ICommand GetResolutionAtPCBCommand { get { return new RelayCommand(GetResolutionAtPCB); } }
        private void GetResolutionAtPCB()
        {
            double cal_target_x = CalTargetModel.TARGET_PCB_POS_X_MM;
            double cal_target_y = CalTargetModel.TARGET_PCB_POS_Y_MM;

            GetResolution(cal_target_x, cal_target_y, Constants.CAL_TYPE_RESOLUTION_AT_PCB, Constants.CAL_TYPE_Z_DISTANCE_AT_PCB);
        }

        public ICommand GetResolutionAtToolCommand { get { return new RelayCommand(GetResolutionAtTool); } }
        private void GetResolutionAtTool()
        {
            double cal_target_x = CalTargetModel.TARGET_TOOL_POS_X_MM;
            double cal_target_y = CalTargetModel.TARGET_TOOL_POS_Y_MM;

            GetResolution(cal_target_x, cal_target_y, Constants.CAL_TYPE_RESOLUTION_AT_TOOL, Constants.CAL_TYPE_Z_DISTANCE_AT_TOOL);
        }
              
        private void GetResolution(double x, double y, char type, char type2)
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            
            //Define Target circle to find
            CircleSegment calCircle = new CircleSegment();
            calCircle.Center = new Point2f((float)x, (float)y);
            calCircle.Radius = ((float)(CalTargetModel.TARGET_RESOLUTION_RADIUS_MILS * Constants.MIL_TO_MM));

            machine.Messages.Add(GCommand.G_IterativeAlignToCircle(calCircle, 4));
            machine.Messages.Add(GCommand.X_SetCalFactor(type));
            machine.Messages.Add(GCommand.G_ProbeZ(24.0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.X_SetCalFactor(type2));
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
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
