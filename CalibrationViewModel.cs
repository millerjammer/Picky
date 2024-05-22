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
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        /* Camera/Pick Physics */

        /**/
        public double MachineOriginToDownCameraX
        {
            get { return machine.Cal.MachineOriginToDownCameraX; }
            set { machine.Cal.MachineOriginToDownCameraX = value; OnPropertyChanged(nameof(MachineOriginToDownCameraX)); }
        }
        
        public double MachineOriginToDownCameraY
        {
            get { return machine.Cal.MachineOriginToDownCameraY; }
            set { machine.Cal.MachineOriginToDownCameraY = value; OnPropertyChanged(nameof(MachineOriginToDownCameraY)); }
        }
        public double MachineOriginToDownCameraZ
        {
            get { return machine.Cal.MachineOriginToDownCameraZ; }
            set { machine.Cal.MachineOriginToDownCameraZ = value; OnPropertyChanged(nameof(MachineOriginToDownCameraZ)); }
        }

        /**/
        public double MachineOriginToPickHeadX1
        {
            get { return machine.Cal.MachineOriginToPickHeadX1; }
            set { machine.Cal.MachineOriginToPickHeadX1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadX1)); }
        }

        public double MachineOriginToPickHeadY1
        {
            get { return machine.Cal.MachineOriginToPickHeadY1; }
            set { machine.Cal.MachineOriginToPickHeadY1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadY1)); }
        }

        public double MachineOriginToPickHeadZ1
        {
            get { return machine.Cal.MachineOriginToPickHeadZ1; }
            set { machine.Cal.MachineOriginToPickHeadZ1 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadZ1)); }
        }

        /**/
        public double MachineOriginToPickHeadX2
        {
            get { return machine.Cal.MachineOriginToPickHeadX2; }
            set { machine.Cal.MachineOriginToPickHeadX2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadX2)); }
        }

        public double MachineOriginToPickHeadY2
        {
            get { return machine.Cal.MachineOriginToPickHeadY2; }
            set { machine.Cal.MachineOriginToPickHeadY2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadY2)); }
        }

        public double MachineOriginToPickHeadZ2
        {
            get { return machine.Cal.MachineOriginToPickHeadZ2; }
            set { machine.Cal.MachineOriginToPickHeadZ2 = value; OnPropertyChanged(nameof(MachineOriginToPickHeadZ2)); }
        }

        /**

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

        public ICommand MOToDC { get { return new RelayCommand(mOToDC); } }
        private void mOToDC()
        {
            MachineOriginToDownCameraX = machine.CurrentX;
            MachineOriginToDownCameraY = machine.CurrentY;
            MachineOriginToDownCameraZ = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        public ICommand MOToPHZ1 { get { return new RelayCommand(mOToPHZ1); } }
        private void mOToPHZ1()
        {
            MachineOriginToPickHeadX1 = machine.CurrentX;
            MachineOriginToPickHeadY1 = machine.CurrentY;
            MachineOriginToPickHeadZ1 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        public ICommand MOToPHZ2 { get { return new RelayCommand(mOToPHZ2); } }
        private void mOToPHZ2()
        {
            MachineOriginToPickHeadX2 = machine.CurrentX;
            MachineOriginToPickHeadY2 = machine.CurrentY;
            MachineOriginToPickHeadZ2 = machine.CurrentZ;
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
        }

        /* This is called when machine z changes */
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Machine.CurrentZ));
            machine.Cal.GetPickHeadOffsetToCamera(Machine.CurrentZ);
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
