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
        public CalTargetModel target { get; set; } 
        
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

        public double DownCameraAngleX
        {
            get { return machine.Cal.DownCameraAngleX; }
            set { machine.Cal.DownCameraAngleX = value; OnPropertyChanged(nameof(DownCameraAngleX)); }
        }

        public double DownCameraAngleY
        {
            get { return machine.Cal.DownCameraAngleY; }
            set { machine.Cal.DownCameraAngleY = value; OnPropertyChanged(nameof(DownCameraAngleY)); }
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
            target = new CalTargetModel();
        }

        public ICommand PerformCalibrationCommand { get { return new RelayCommand(PerformCalibration); } }
        private void PerformCalibration()
        {
            target.PerformCalibration(machine);
        }

        public ICommand GoMonument00Command { get { return new RelayCommand(GoMonument00); } }
        private void GoMonument00()
        {
            machine.Messages.Add(GCommand.G_SetPosition(target.Grid00Location.X, target.Grid00Location.Y, 0, 0, 0));
        }

        public ICommand GoMonument11Command { get { return new RelayCommand(GoMonument11); } }
        private void GoMonument11()
        {
            machine.Messages.Add(GCommand.G_SetPosition(target.Grid11Location.X, target.Grid11Location.Y, 0, 0, 0));
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

        public ICommand MOToDC { get { return new RelayCommand(mOToDC); } }
        private void mOToDC()
        {
            machine.Cal.MachineOriginToDownCameraX = machine.CurrentX;
            machine.Cal.MachineOriginToDownCameraY = machine.CurrentY;
            machine.Cal.MachineOriginToDownCameraZ = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        public ICommand MOToPHZ1 { get { return new RelayCommand(mOToPHZ1); } }
        private void mOToPHZ1()
        {
            machine.Cal.MachineOriginToPickHeadX1 = machine.CurrentX;
            machine.Cal.MachineOriginToPickHeadY1 = machine.CurrentY;
            machine.Cal.MachineOriginToPickHeadZ1 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        public ICommand MOToPHZ2 { get { return new RelayCommand(mOToPHZ2); } }
        private void mOToPHZ2()
        {
            machine.Cal.MachineOriginToPickHeadX2 = machine.CurrentX;
            machine.Cal.MachineOriginToPickHeadY2 = machine.CurrentY;
            machine.Cal.MachineOriginToPickHeadZ2 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        /* This is called when machine z changes */
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Machine.CurrentZ));
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
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

        public ICommand GetResolutionAtFeederCommand { get { return new RelayCommand(GetResolutionAtFeeder); } }
        private void GetResolutionAtFeeder()
        {
        }

        private void GetResolution(double x, double y, byte type, byte type2)
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));

            CircleSegment calCircle = new CircleSegment();
            calCircle.Center = new Point2f((float)x, (float)y);
            calCircle.Radius = ((float)(CalTargetModel.TARGET_RESOLUTION_RADIUS_MILS * Constants.MIL_TO_MM));
            machine.Messages.Add(GCommand.G_IterativeAlignToCircle(calCircle, target.CalCircle, 10));
            machine.Messages.Add(GCommand.X_SetCalFactor(type));
            machine.Messages.Add(GCommand.G_ProbeZ(300));
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
