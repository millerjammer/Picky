using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls.Primitives;

namespace Picky
{
    public sealed class MachineModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        /* Declare as Singleton */
        private static readonly Lazy<MachineModel> lazy = new Lazy<MachineModel>(() => new MachineModel());
                
        public RelayInterface relayInterface;

        /* Serial Message Queue */
        public ObservableCollection<MachineMessage> Messages { get; set; }

        private MachineMessage selectedMachineMessage;
        public MachineMessage SelectedMachineMessage
        {
            get { return selectedMachineMessage; }
            set { selectedMachineMessage = value; OnPropertyChanged(nameof(SelectedMachineMessage)); }
        }

        public bool isMachinePaused { get; set; }
                
        /* Calibration Stuff */
        public CalibrationModel Cal {  get; set; }

        /* Cameras */
        public CameraModel upCamera { get; set; }
        public CameraModel downCamera { get; set; }

        /* These are temporary for use while performing calibration */
        public PickToolModel CalPick { get; set; } = new PickToolModel();
        public OpenCvSharp.Rect CalRectangle { get; set; } = new OpenCvSharp.Rect();
       
        /* Current PickTools */
        public ObservableCollection<PickToolModel> PickToolList { get; set; }
        public PickToolModel SelectedPickTool
        {
            get { return Cal.PickToolCal; }
            set { Cal.PickToolCal = value; OnPropertyChanged(nameof(selectedCassette)); }
        }

        /* Current PickList (These are the parts to place) */
        private Part _selectedPickListPart;
        public Part selectedPickListPart
        {
            get { return _selectedPickListPart; }
            set { _selectedPickListPart = value; OnPropertyChanged(nameof(selectedPickListPart)); }
        }
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
        private double currentZ = 0;
        public double CurrentZ
        {
            get { return currentZ; }
            set { currentZ = value; OnPropertyChanged(nameof(CurrentZ)); }
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

        /* Hardware Components */
        private bool isIlluminatorActive;
        public bool IsIlluminatorActive
        {
            get { return isIlluminatorActive; }
            set { isIlluminatorActive = value; OnPropertyChanged(nameof(IsIlluminatorActive)); }
        }

        private bool isUpIlluminatorActive;
        public bool IsUpIlluminatorActive
        {
            get { return isUpIlluminatorActive; }
            set { isUpIlluminatorActive = value; OnPropertyChanged(nameof(IsUpIlluminatorActive)); }
        }

        private bool isPumpActive;
        public bool IsPumpActive
        {
            get { return isPumpActive; }
            set { isPumpActive = value; OnPropertyChanged(nameof(IsPumpActive)); }
        }

        private bool isValveActive;
        public bool IsValveActive
        {
            get { return isValveActive; }
            set { isValveActive = value; OnPropertyChanged(nameof(IsValveActive)); }
        }

        private bool isToolStorageOpen;
        public bool IsToolStorageOpen
        {
            get { return isToolStorageOpen; }
            set { isToolStorageOpen = value; OnPropertyChanged(nameof(IsToolStorageOpen)); }
        }

        private bool isCameraCalibrated;
        public bool IsCameraCalibrated
        {
            get { return isCameraCalibrated; }
            set { isCameraCalibrated = value; OnPropertyChanged(nameof(IsCameraCalibrated)); }
        }

        /* Calibration */
        public enum CalibrationState { NotCalibrated, InProcess, Complete, Failed }
        private CalibrationState cameraCalibrationState;
        public CalibrationState CameraCalibrationState
        {
            get { return cameraCalibrationState; }
            set { cameraCalibrationState = value; OnPropertyChanged(nameof(CameraCalibrationState)); }
        }
        
        private CalibrationState positionCalibrationState;
        public CalibrationState PositionCalibrationState
        {
            get { return positionCalibrationState; }
            set { positionCalibrationState = value; OnPropertyChanged(nameof(PositionCalibrationState)); }
        }

        private CalibrationState pickCalibrationState;
        public CalibrationState PickCalibrationState
        {
            get { return pickCalibrationState; }
            set { pickCalibrationState = value; OnPropertyChanged(nameof(PickCalibrationState)); }
        }

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

            downCamera = new CameraModel(2);
            upCamera = new CameraModel(0);


            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        
            using (StreamReader file = File.OpenText(path + "\\" + Constants.CALIBRATION_FILE_NAME))
            {
                JsonSerializer serializer = new JsonSerializer();
                Cal = ((CalibrationModel)serializer.Deserialize(file, typeof(CalibrationModel)));
            }

            try
            {
                using (StreamReader file = File.OpenText(path + "\\" + Constants.TOOL_FILE_NAME))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    PickToolList = ((ObservableCollection<PickToolModel>)serializer.Deserialize(file, typeof(ObservableCollection<PickToolModel>)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't find file: " + path + "\\" + Constants.TOOL_FILE_NAME);
                
                PickToolList = new ObservableCollection<PickToolModel>();
                PickToolList.Add(new PickToolModel("Test 1"));
                PickToolList.Add(new PickToolModel("Test 2"));
                PickToolList.Add(new PickToolModel("Test 3"));
            }
        }

        public static MachineModel Instance
        {
            get
            {
                return lazy.Value;
            }
        }
        
        public double GetImageScaleAtDistanceX(double distance)
        {
            /*******************************************************************************/
            /* Returns mm/pix given distance (as reported by Machine i.e. machine.CurrentX */
            /* Use law of similar triangles (caldist/calscale) = (newdist/?) solve for ?   */

            /* Get mm/pix */
            double calibrationScale = ((Constants.CALIBRATION_TARGET_WIDTH_MM) / Cal.RefObject.Width);
            double distanceScale = ((distance + Constants.CAMERA_OFFSET_Z) * calibrationScale) / (Constants.CAMERA_OFFSET_Z + Constants.CALIBRATION_TARGET_DIST_MM);
            return (distanceScale);
        }

        public double GetImageScaleAtDistanceY(double distance)
        {
            /*******************************************************************************/
            /* Returns mm/pix given distance (as reported by Machine i.e. machine.CurrentX */
            /* Use law of similar triangles (caldist/calscale) = (newdist/?) solve for ?   */

            /* Get mm/pix */
            double calibrationScale = ((Constants.CALIBRATION_TARGET_HEIGHT_MM) / Cal.RefObject.Height);
            double distanceScale = ((distance + Constants.CAMERA_OFFSET_Z) * calibrationScale) / (Constants.CAMERA_OFFSET_Z + Constants.CALIBRATION_TARGET_DIST_MM);
            return (distanceScale);
        }

        public bool SetCalPickTool(PickToolModel pickModelToUse)
        {
            Cal.PickToolCal = pickModelToUse;
            // TODO Return false if the pickmodel is bad
            return true;
        }

        public bool SetCalRectangle(OpenCvSharp.Rect rectangleToUse)
        {
            Cal.RefObject = rectangleToUse;
            // TODO Return false if the rectangle is bad
            return true;
        }

        public void SaveSettings()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            File.WriteAllText(path + "\\" + Constants.CALIBRATION_FILE_NAME, JsonConvert.SerializeObject(Cal, Formatting.Indented));
            Console.WriteLine("Configuration Data Saved.");

            File.WriteAllText(path + "\\" + Constants.TOOL_FILE_NAME, JsonConvert.SerializeObject(PickToolList, Formatting.Indented));
            Console.WriteLine("Configuration Data Saved.");
        }
    }
}
