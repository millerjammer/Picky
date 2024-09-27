using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace Picky
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        MachineModel machine;

        public SettingsViewModel(MachineModel mm)
        {
            machine = mm;
            machine.Settings.PropertyChanged += SettingsPropertyChanged;
        }

        public int RateXY
        {
            get { return machine.Settings.RateXY; }
            set { machine.Settings.RateXY = value; }
        }

        public int ProbeRate
        {
            get { return machine.Settings.ProbeRate; }
            set { machine.Settings.ProbeRate = value; }
        }

        public int FeederRate
        {
            get { return machine.Settings.FeederRate; }
            set { machine.Settings.FeederRate = value; }
        }

        public int RotationRate
        {
            get { return machine.Settings.RotationRate; }
            set { machine.Settings.RotationRate = value; }
        }

        public string Response
        {
            get { return machine.Settings.Response; }
            set { machine.Settings.Response = value; }
        }

        public string GCodeCommand
        {
            get { return machine.Settings.GCodeCommand; }
            set { machine.Settings.GCodeCommand = value; }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Propagate the model's property change to the ViewModel's listeners
            OnPropertyChanged(e.PropertyName);
        }

        public ICommand SaveSettingsCommand { get { return new RelayCommand(machine.SaveSettings); } }
        

        public ICommand ReadActiveSettingsCommand { get { return new RelayCommand(ReadSettings); } }
        private void ReadSettings()
        {
            Console.WriteLine("Read Settings");
            machine.Settings.Response = null;
            machine.Messages.Add(GCommand.G_ReportSettings());
        }

        public ICommand WriteActiveSettingsCommand { get { return new RelayCommand(WriteSettings); } }
        private void WriteSettings()
        {
            Console.WriteLine("Write Settings");
            machine.Messages.Add(GCommand.G_SaveSettings());
        }

        public ICommand SendCustomCommand { get { return new RelayCommand(SendCustom); } }
        private void SendCustom()
        {
            Console.WriteLine("Send Custom");
            machine.Settings.Response = null;
            machine.Messages.Add(GCommand.SendCustomGCode(GCodeCommand, 2000));
        }
               

    }
}
