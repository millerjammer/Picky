using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class SettingsModel : INotifyPropertyChanged
    {

        private int rateXY;
        public int RateXY
        {
            get { return rateXY; }
            set { rateXY = value; OnPropertyChanged(nameof(RateXY)); }
        }

        private int probeRate;
        public int ProbeRate
        {
            get { return probeRate; }
            set { probeRate = value; OnPropertyChanged(nameof(ProbeRate)); }
        }

        private int feederRate;
        public int FeederRate
        {
            get { return feederRate; }
            set { feederRate = value; OnPropertyChanged(nameof(FeederRate)); }
        }

        private int rotationRate;
        public int RotationRate
        {
            get { return rotationRate; }
            set { rotationRate = value; OnPropertyChanged(nameof(RotationRate)); }
        }


        private string response;
        public string Response
        { 
            get { return response; }
            set { response = value; OnPropertyChanged(nameof(Response)); }
        }

        private string gCodeCommand;
        public string GCodeCommand
        {
            get { return gCodeCommand; }
            set { gCodeCommand = value; OnPropertyChanged(nameof(GCodeCommand)); }
        }


        public SettingsModel() { 
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}