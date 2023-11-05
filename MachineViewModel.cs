﻿using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Picky
{

    public class MachineViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
           get { return machine; }
           set { OnPropertyChanged(nameof(Machine)); }
        }
        

        /* For controls only */
        public double distanceToAdvance;
        private double[] distToAdvValue = new double[] { 0.1, 1.0, 10.0, 100.0 };
        private bool[] DistToAdv = new bool[] { false, true, false, false };
        public bool[] distToAdv
        {
            get { int i = Array.IndexOf(DistToAdv, true); Console.WriteLine("Default: " + i); distanceToAdvance = distToAdvValue[i]; return DistToAdv; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MachineViewModel()
        {
            machine.Messages.Add(Command.S3G_Initialize());
            machine.Messages.Add(Command.S3G_Initialize());
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.relayInterface.SetIlluminatorOn(true);
            
        }

        public ICommand ButtonXLeftCommand { get { return new RelayCommand(ButtonXLeft); } }
        private void ButtonXLeft()
        {
            machine.Messages.Add(Command.S3G_SetRelativeXYPosition(-distanceToAdvance, 0));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonXRightCommand { get { return new RelayCommand(ButtonXRight); } }
        private void ButtonXRight()
        {
            machine.Messages.Add(Command.S3G_SetRelativeXYPosition(distanceToAdvance, 0));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonYUpCommand { get { return new RelayCommand(ButtonYUp); } }
        private void ButtonYUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeXYPosition(0, distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonYDownCommand { get { return new RelayCommand(ButtonYDown); } }
        private void ButtonYDown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeXYPosition(0, -distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonXYHomeCommand { get { return new RelayCommand(ButtonXYHome); } }
        private void ButtonXYHome()
        {
            machine.Messages.Add(Command.S3G_Initialize());
            machine.Messages.Add(Command.S3G_FindXYMaximums());
            machine.Messages.Add(Command.S3G_GetPosition());
        }


        public ICommand ButtonZUpCommand { get { return new RelayCommand(ButtonZUp); } }
        private void ButtonZUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeZPosition(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonZHomeCommand { get { return new RelayCommand(ButtonZHome); } }
        private void ButtonZHome()
        {
            machine.Messages.Add(Command.S3G_FindZMinimum());
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonZDownCommand { get { return new RelayCommand(ButtonZDown); } }
        private void ButtonZDown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeZPosition(-distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBUpCommand { get { return new RelayCommand(ButtonBUp); } }
        private void ButtonBUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAngle(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBHomeCommand { get { return new RelayCommand(ButtonBHome); } }
        private void ButtonBHome()
        {
            machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0.00));
            machine.Messages.Add(Command.S3G_GetPosition());
        }
        public ICommand ButtonBDownCommand { get { return new RelayCommand(ButtonBDown); } }
        private void ButtonBDown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAngle(-distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAUpCommand { get { return new RelayCommand(ButtonAUp); } }
        private void ButtonAUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAPosition(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAHomeCommand { get { return new RelayCommand(ButtonAHome); } }
        void ButtonAHome()
        {

        }

        public ICommand ButtonADownCommand { get { return new RelayCommand(ButtonADown); } }
        private void ButtonADown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAPosition(-distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand EditPickToolCommand { get { return new RelayCommand(EditPickTool); } }
        private void EditPickTool()
        {
            Console.WriteLine("Edit Pick Tool");
        }

        public ICommand GoToPCBOriginCommand { get { return new RelayCommand(GoToPCBOrigin); } }
        private void GoToPCBOrigin()
        {
            Console.WriteLine("GoTo PCB");
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(machine.PCB_OriginX, machine.PCB_OriginY));
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(machine.PCB_OriginZ));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0));
            machine.Messages.Add(Command.S3G_GetPosition());
        }
        
        public ICommand SetAsPCBOriginCommand { get { return new RelayCommand(SetAsPCBOrigin); } }
        private void SetAsPCBOrigin()
        {
            Console.WriteLine("SetAs PCB Origin");
            machine.PCB_OriginX = machine.CurrentX;
            machine.PCB_OriginY = machine.CurrentY;
            machine.PCB_OriginZ = machine.CurrentZ;
            machine.SaveCalibrationSettings();

        }
    }
}
