using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public sealed class MachineModel : INotifyPropertyChanged
    {
        /* Declare as Singleton */
        private static readonly Lazy<MachineModel> lazy = new Lazy<MachineModel>(() => new MachineModel());

        public RelayInterface relayInterface;
        
        /* Serial Message Queue */
        public ObservableCollection<MachineMessage> Messages { get; set; }

        /* Cassetts that are installed */
        public ObservableCollection<Cassette> Cassettes { get; set; }

        public int LastEndStopState { get; set; } = 0;
        private string calibrationStatusString = "Not Calibrated";
        public string CalibrationStatusString
        {
            get { return calibrationStatusString; }
            set { calibrationStatusString = value; OnPropertyChanged(nameof(CalibrationStatusString)); }
        }

        /* Current machine position - needed because serial port makes changes here */
        private double currentX;
        public double CurrentX
        {
            get { return currentX; }
            set { currentX = value; OnPropertyChanged(nameof(CurrentX)); }
        }
        private double currentY;
        public double CurrentY
        {
            get { return currentY; }
            set { currentY = value; OnPropertyChanged(nameof(CurrentY)); }
        }
        private double currentZ;
        public double CurrentZ
        {
            get { return currentZ; }
            set { currentZ = value; OnPropertyChanged(nameof(CurrentZ)); }
        }
        private double currentA;
        public double CurrentA
        {
            get { return currentA; }
            set { currentA = value; OnPropertyChanged(nameof(CurrentA)); }
        }
        private double currentB;
        public double CurrentB
        {
            get { return currentB; }
            set { currentB = value; OnPropertyChanged(nameof(CurrentB)); }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MachineModel()
        {
            Cassettes = new ObservableCollection<Cassette>();
            Messages = new ObservableCollection<MachineMessage>();

            relayInterface = new RelayInterface();
        
        }
        public static MachineModel Instance
        {
            get
            {
                return lazy.Value;
            }
        }

    }
        
}
