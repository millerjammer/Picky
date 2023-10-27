using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Picky
{
    public sealed class MachineModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        /* Declare as Singleton */
        private static readonly Lazy<MachineModel> lazy = new Lazy<MachineModel>(() => new MachineModel());

        public RelayInterface relayInterface;

        /* Serial Message Queue */
        public ObservableCollection<MachineMessage> Messages { get; set; }

        public Mat currentRawImage;

        public bool isMachinePaused { get; set; }

        /* Current PickList */
        public Part selectedPickListPart { get; set; }
        public ObservableCollection<Part> PickList { get; set; }

        /* Cassettes that are installed */
        private Cassette _selectedCassette;
        public Cassette selectedCassette
        {
            get { return _selectedCassette; }
            set { _selectedCassette = value; OnPropertyChanged(nameof(selectedCassette)); }
        }
        public ObservableCollection<Cassette> Cassettes { get; set; } 

        public int LastEndStopState { get; set; } = 0;
        private string calibrationStatusString = "Not Calibrated";
        public string CalibrationStatusString
        {
            get { return calibrationStatusString; }
            set { calibrationStatusString = value; OnPropertyChanged(nameof(CalibrationStatusString)); }
        }

        /* Current machine position - needed because serial port makes changes here */
        private double currentX = 0;
        public double CurrentX
        {
            get { return currentX; }
            set { currentX = value; OnPropertyChanged(nameof(CurrentX)); }
        }
        private double currentY = 0;
        public double CurrentY
        {
            get { return currentY; }
            set { currentY = value; OnPropertyChanged(nameof(CurrentY)); }
        }
        private double currentZ =0;
        public double CurrentZ
        {
            get { return currentZ; }
            set { 
                currentZ = value;
                CurrentFrameXDimension = (7.73 * (Constants.CAMERA_OFFSET_Z + currentZ)) / (Constants.CAMERA_OFFSET_Z + 25.0);
                CurrentFrameYDimension = (5.71 * (Constants.CAMERA_OFFSET_Z + currentZ)) / (Constants.CAMERA_OFFSET_Z + 25.0);
                Console.WriteLine("Frame Dims: " + CurrentFrameXDimension + "mm " + CurrentFrameYDimension + "mm");
                OnPropertyChanged(nameof(CurrentZ)); 
            }
        }
        private double currentA = 0;
        public double CurrentA
        {
            get { return currentA; }
            set { currentA = value; OnPropertyChanged(nameof(CurrentA)); }
        }
        private double currentB = 0;
        public double CurrentB
        {
            get { return currentB; }
            set { currentB = value; OnPropertyChanged(nameof(CurrentB)); }
        }

        private bool isCameraCalibrated;
        public bool IsCameraCalibrated
        {
            get { return isCameraCalibrated; }
            set { isCameraCalibrated = value; OnPropertyChanged(nameof(IsCameraCalibrated)); }
        }

        private bool isXYCalibrated;
        public bool IsXYCalibrated
        {
            get { return isXYCalibrated; }
            set { isXYCalibrated = value; OnPropertyChanged(nameof(IsXYCalibrated)); }
        }
        private bool isZCalibrated;
        public bool IsZCalibrated
        {
            get { return isZCalibrated; }
            set { isZCalibrated = value; OnPropertyChanged(nameof(IsZCalibrated)); }
        }

        public double CurrentFrameXDimension { get; set; }
        public double CurrentFrameYDimension { get; set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("feeder collection changed");
            OnPropertyChanged(nameof(Cassettes)); // Notify that the collection has changed
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MachineModel()
        {
            Messages = new ObservableCollection<MachineMessage>();
            Cassettes = new ObservableCollection<Cassette>();
            PickList = new ObservableCollection<Part>();
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
