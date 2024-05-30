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
        Point2d itemLocation = new Point2d();

        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        /* This is derived from above */
        private double DownCameraToPickHeadX
        {
            get { return machine.Cal.DownCameraToPickHeadX; }
            set { machine.Cal.DownCameraToPickHeadX = value; OnPropertyChanged(nameof(DownCameraToPickHeadX)); }
        }

        private double DownCameraToPickHeadY
        {
            get { return machine.Cal.DownCameraToPickHeadY; }
            set { machine.Cal.DownCameraToPickHeadY = value; OnPropertyChanged(nameof(DownCameraToPickHeadY)); }
        }

        private double DownCameraAngleX
        {
            get { return machine.Cal.DownCameraAngleX; }
            set { machine.Cal.DownCameraAngleX = value; OnPropertyChanged(nameof(DownCameraAngleX)); }
        }

        private double DownCameraAngleY
        {
            get { return machine.Cal.DownCameraAngleY; }
            set { machine.Cal.DownCameraAngleY = value; OnPropertyChanged(nameof(DownCameraAngleY)); }
        }

        private double StepsPerUnitX
        {
            get { return machine.Cal.StepsPerUnitX; }
            set { machine.Cal.StepsPerUnitX = value; OnPropertyChanged(nameof(StepsPerUnitX)); }
        }

        private double StepsPerUnitY
        {
            get { return machine.Cal.StepsPerUnitY; }
            set { machine.Cal.StepsPerUnitY = value; OnPropertyChanged(nameof(StepsPerUnitY)); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CalibrationViewModel(MachineModel machineCal)
        {
            machine = machineCal;
            machine.PropertyChanged += OnMachinePropertyChanged;

        }
        
        public ICommand GoMonument00Command { get { return new RelayCommand(GoMonument00); } }
        private void GoMonument00()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Monument00X, machine.Cal.Monument00Y, 0, 0, 0));
                      
        }
        
        public ICommand GoMonument11Command { get { return new RelayCommand(GoMonument11); } }
        private void GoMonument11()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Monument11X, machine.Cal.Monument11Y, 0, 0, 0));
        }

        public ICommand GetMonument00Command { get { return new RelayCommand(GetMonument00); } }
        private void GetMonument00()
        {
            machine.Cal.Monument00X = machine.CurrentX;
            machine.Cal.Monument00Y = machine.CurrentY;
          
        }

        public ICommand GetMonument11Command { get { return new RelayCommand(GetMonument11); } }
        private void GetMonument11()
        {
            machine.Cal.Monument11X = machine.CurrentX;
            machine.Cal.Monument11Y = machine.CurrentY;
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
            double cal_target_x = 238.48;
            double cal_target_y = 127.690;

            GetResolution(cal_target_x, cal_target_y, Constants.CAL_TYPE_RESOLUTION_AT_PCB, Constants.CAL_TYPE_Z_DISTANCE_AT_PCB);
        }

        public ICommand GetResolutionAtToolCommand { get { return new RelayCommand(GetResolutionAtTool); } }
        private void GetResolutionAtTool()
        {
            double cal_target_x = 238.48;
            double cal_target_y = 127.690;

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
            machine.Messages.Add(GCommand.C_LocateCircle(new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT)));
            machine.Messages.Add(GCommand.X_SetCalFactor(type));
            machine.Messages.Add(GCommand.G_ProbeZ(300));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.X_SetCalFactor(type2));
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetItemLocation());
            machine.Messages.Add(GCommand.G_EnableIlluminator(false));
        }
                
        public ICommand MoveToPickLocation { get { return new RelayCommand(moveToPickLocation); } }
        private void moveToPickLocation()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(DownCameraToPickHeadX, DownCameraToPickHeadY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand MoveToCameraLocation { get { return new RelayCommand(moveToCameraLocation); } }
        private void moveToCameraLocation()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(-DownCameraToPickHeadX, -DownCameraToPickHeadY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand OkCommand { get { return new RelayCommand(okCommand); } }
        private void okCommand()
        {
            machine.SaveSettings();
        }
    }
}
