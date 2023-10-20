using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Interop;
using Wpf.Ui.Input;

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
            machine.Messages.Add(Command.S3G_SetRelativeZPosition(-distanceToAdvance));
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
            machine.Messages.Add(Command.S3G_SetRelativeZPosition(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAUpCommand { get { return new RelayCommand(ButtonAUp); } }
        private void ButtonAUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAngle(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAHomeCommand { get { return new RelayCommand(ButtonAHome); } }
        private void ButtonAHome()
        {

        }
        public ICommand ButtonADownCommand { get { return new RelayCommand(ButtonADown); } }
        private void ButtonADown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeAngle(-distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBUpCommand { get { return new RelayCommand(ButtonBUp); } }
        private void ButtonBUp()
        {
            machine.Messages.Add(Command.S3G_SetRelativeBPosition(distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBHomeCommand { get { return new RelayCommand(ButtonBHome); } }
        void ButtonBHome()
        {

        }

        public ICommand ButtonBDownCommand { get { return new RelayCommand(ButtonBDown); } }
        private void ButtonBDown()
        {
            machine.Messages.Add(Command.S3G_SetRelativeBPosition(-distanceToAdvance));
            machine.Messages.Add(Command.S3G_GetPosition());
        }
    }
}
