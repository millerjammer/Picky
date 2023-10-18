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
        private readonly MachineModel model;
        public readonly RelayInterface relayInterface;
        public readonly SerialInterface serialInterface;

        public double CurrentX
        {
           get { return model.CurrentX; }
           set { model.CurrentX = value; OnPropertyChanged(nameof(CurrentX)); }
        }
        public double CurrentY
        {
            get { return model.CurrentY; }
            set { model.CurrentY = value; OnPropertyChanged(nameof(CurrentY)); }
        }
        public double CurrentZ
        {
            get { return model.CurrentZ; }
            set { model.CurrentZ = value; OnPropertyChanged(nameof(CurrentZ)); }
        }
        public double CurrentA
        {
            get { return model.CurrentA; }
            set { model.CurrentA = value; OnPropertyChanged(nameof(CurrentA)); }
        }
        public double CurrentB
        {
            get { return model.CurrentB; }
            set { model.CurrentB = value; OnPropertyChanged(nameof(CurrentB)); }
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

        public MachineViewModel(MachineModel mModel)
        {
            model = mModel;
            relayInterface = new RelayInterface();
            serialInterface = new SerialInterface(this);

        }

        public ICommand ButtonXLeftCommand { get { return new RelayCommand(ButtonXLeft); } }
        private void ButtonXLeft()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeXYPosition(-distanceToAdvance, 0));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonXRightCommand { get { return new RelayCommand(ButtonXRight); } }
        private void ButtonXRight()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeXYPosition(distanceToAdvance, 0));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonYUpCommand { get { return new RelayCommand(ButtonYUp); } }
        private void ButtonYUp()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeXYPosition(0, distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonYDownCommand { get { return new RelayCommand(ButtonYDown); } }
        private void ButtonYDown()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeXYPosition(0, -distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonXYHomeCommand { get { return new RelayCommand(ButtonXYHome); } }
        private void ButtonXYHome()
        {
            serialInterface.Messages.Add(Command.S3G_Initialize());
            serialInterface.Messages.Add(Command.S3G_FindXYMaximums());
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }


        public ICommand ButtonZUpCommand { get { return new RelayCommand(ButtonZUp); } }
        private void ButtonZUp()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeZPosition(-distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonZHomeCommand { get { return new RelayCommand(ButtonZHome); } }
        private void ButtonZHome()
        {
            serialInterface.Messages.Add(Command.S3G_FindZMinimum());
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonZDownCommand { get { return new RelayCommand(ButtonZDown); } }
        private void ButtonZDown()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeZPosition(distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAUpCommand { get { return new RelayCommand(ButtonAUp); } }
        private void ButtonAUp()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeAngle(distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonAHomeCommand { get { return new RelayCommand(ButtonAHome); } }
        private void ButtonAHome()
        {

        }
        public ICommand ButtonADownCommand { get { return new RelayCommand(ButtonADown); } }
        private void ButtonADown()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeAngle(-distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBUpCommand { get { return new RelayCommand(ButtonBUp); } }
        private void ButtonBUp()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeBPosition(distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand ButtonBHomeCommand { get { return new RelayCommand(ButtonBHome); } }
        void ButtonBHome()
        {

        }

        public ICommand ButtonBDownCommand { get { return new RelayCommand(ButtonBDown); } }
        private void ButtonBDown()
        {
            serialInterface.Messages.Add(Command.S3G_SetRelativeBPosition(-distanceToAdvance));
            serialInterface.Messages.Add(Command.S3G_GetPosition());
        }
    }
}
