using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    public class ControlViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;

        public bool isIlluminatorOn { get; set; }
        public bool isPumpOn { get; set; }
        public bool isValveOn { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public ControlViewModel() {
            machine.Messages.Add(Command.S3G_GetPosition());
        }
        public ICommand InitializeCommand { get { return new RelayCommand(Initialize); } }
        private void Initialize()
        {
            Console.WriteLine("init"); 
        }
        public ICommand HomeCommand { get { return new RelayCommand(Home); } }
        private void Home()
        {
            // First, ensure the needle is safe
            machine.Messages.Add(Command.S3G_SetRelativeZPosition(30));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_FindXYMaximums());
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.JRM_CalibrationCheckXY());
            machine.Messages.Add(Command.S3G_FindZMinimum());
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetPositionAs(0, 0, 0, 0, 0));
            machine.Messages.Add(Command.JRM_CalibrationCheckZ());
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0));
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(-50, -50));
            machine.Messages.Add(Command.S3G_GetPosition());
        }
        public ICommand SelfCheckCommand { get { return new RelayCommand(SelfCheck); } }
        private void SelfCheck()
        {

        }
        public ICommand StartCommand { get { return new RelayCommand(Start); } }
        private void Start()
        {

        }
        public ICommand PauseCommand { get { return new RelayCommand(Pause); } }
        private void Pause()
        {

        }
        public ICommand StopCommand { get { return new RelayCommand(Stop); } }
        private void Stop()
        {
            machine.Messages.Add(Command.S3G_EnableSteppers((byte)(Constants.A_AXIS | Constants.B_AXIS | Constants.X_AXIS | Constants.Y_AXIS | Constants.Z_AXIS), false));
            machine.CalibrationStatusString = "Re-Calibration Required";
        }
        public ICommand IlluminatorToggleCommand { get { return new RelayCommand(IlluminatorToggle); } }
        private void IlluminatorToggle()
        {
            machine.relayInterface.SetIlluminatorOn(isIlluminatorOn);
        }
        public ICommand PumpToggleCommand { get { return new RelayCommand(PumpToggle); } }
        private void PumpToggle()
        {
            machine.relayInterface.SetPumpOn(isPumpOn);
        }
        public ICommand ValveToggleCommand { get { return new RelayCommand(ValveToggle); } }
        private void ValveToggle()
        {
            machine.relayInterface.SetValveOn(isValveOn);
        }
    }
}
