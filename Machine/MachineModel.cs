using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls.WebParts;

namespace Picky
{
    public sealed class MachineModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        /* Declare as Singleton */
        private static readonly Lazy<MachineModel> lazy = new Lazy<MachineModel>(() => new MachineModel());

        /* Serial Message Queue */
        private ObservableCollection<MachineMessage> messages;
        public ObservableCollection<MachineMessage> Messages
        {
            get { return messages; }
            set { messages = value; OnPropertyChanged(nameof(Messages)); }
        }

        private MachineMessage selectedMachineMessage;
        public MachineMessage SelectedMachineMessage
        {
            get { return selectedMachineMessage; }
            set { selectedMachineMessage = value; OnPropertyChanged(nameof(SelectedMachineMessage)); }
        }
                
        public bool advanceNextMessage = false;
        public bool isMachinePaused { get; set; }
        public bool IsSerialMessageResetRequested { get; set; }
       
        /* Calibration */
        public CalibrationModel Cal { get; set; }

        /* Board */
        public BoardModel Board { get; set; }

        /* Settings */
        public SettingsModel Settings { get; set; }

        /* Cameras */
        public CameraModel upCamera { get; set; }
        public CameraModel downCamera { get; set; }

        /* Tools */
        public ObservableCollection<PickToolModel> PickToolList { get; set; }

        private PickToolModel selectedPickTool;
        public PickToolModel SelectedPickTool
        {
            get { return selectedPickTool; }
            set { selectedPickTool = value; OnPropertyChanged(nameof(SelectedPickTool)); }
        }

        /* Current PickList (These are the parts to place) */
        public ObservableCollection<Part> PickList { get; set; }
        
        private Part _selectedPickListPart;
        public Part selectedPickListPart
        {
            get { return _selectedPickListPart; }
            set { _selectedPickListPart = value; OnPropertyChanged(nameof(selectedPickListPart)); }
        }
        
        /* Cassettes that are installed */
        private Cassette _selectedCassette;
        public Cassette selectedCassette
        {
            get { return _selectedCassette; }
            set { _selectedCassette = value; OnPropertyChanged(nameof(selectedCassette)); }
        }
        public ObservableCollection<Cassette> Cassettes { get; set; }

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

        private int rxMessageCount = 0;
        public int RxMessageCount
        {
            get { return rxMessageCount; }
            set { rxMessageCount = value; OnPropertyChanged(nameof(RxMessageCount)); }
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

            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            try
            {
                using (StreamReader file = File.OpenText(path + "\\" + Constants.SETTINGS_FILE_NAME))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Settings = ((SettingsModel)serializer.Deserialize(file, typeof(SettingsModel)));
                }
            }
            catch
            {
                Console.WriteLine("Can't find file: " + path + "\\" + Constants.SETTINGS_FILE_NAME);
                Settings = new SettingsModel();
            }

            try
            {
                using (StreamReader file = File.OpenText(path + "\\" + Constants.BOARD_FILE_NAME))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Board = ((BoardModel)serializer.Deserialize(file, typeof(BoardModel)));
                }
            }
            catch
            {
                Console.WriteLine("Can't find file: " + path + "\\" + Constants.BOARD_FILE_NAME);
                Board = new BoardModel();
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
            }
            
            
            downCamera = new CameraModel(Constants.DOWN_CAMERA_INDEX, this);
            upCamera = new CameraModel(Constants.UP_CAMERA_INDEX, this);


            path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

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
            }
            foreach (var item in PickToolList)
            {
                item.TipState = PickToolModel.TipStates.Unknown;
                
            }
        }

        public static MachineModel Instance
        {
            get { return lazy.Value; }
        }
        
        public void SaveSettings()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(path + "\\" + Constants.SETTINGS_FILE_NAME, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            Console.WriteLine("Save Settings");
        }

        public void SaveTools()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(path + "\\" + Constants.TOOL_FILE_NAME, JsonConvert.SerializeObject(PickToolList, Formatting.Indented));
            Console.WriteLine("Tool Configuration Data Saved.");
        }

        public void SaveCalibration()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(path + "\\" + Constants.CALIBRATION_FILE_NAME, JsonConvert.SerializeObject(Cal, Formatting.Indented));
            Console.WriteLine("Calibration Configuration Data Saved.");

        }
        public bool AddFeederPickToQueue(Feeder feeder)
        {
            //Go to feeder Origin
            Messages.Add(GCommand.SetCameraManualFocus(downCamera, true, Constants.FOCUS_FEEDER_QR_CODE));
            Messages.Add(GCommand.G_SetPosition(feeder.x_origin, feeder.y_origin, 0, 0, 0));
            Messages.Add(GCommand.G_FinishMoves());
            Messages.Add(GCommand.SetCameraManualFocus(downCamera, true, Constants.FOCUS_FEEDER_PART));
            Messages.Add(GCommand.OpticallyAlignToPart(feeder.part));
            Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            Messages.Add(GCommand.OffsetCameraToPick(feeder.part));
            Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            Messages.Add(GCommand.G_ProbeZ(Constants.PART_NOMINAL_Z_DRIVE_MM));
            Messages.Add(GCommand.G_FinishMoves());
            Messages.Add(GCommand.G_EnablePump(true));
            Messages.Add(GCommand.G_EnableValve(false));
            Messages.Add(GCommand.Delay(100));
            Messages.Add(GCommand.G_SetZPosition(0));
            Messages.Add(GCommand.G_FinishMoves());
            return true;
        }

        public bool AddPartPlacementToQueue(Part part)
        { /* Sets PICK location */
            double part_x = Board.PcbOriginX + (Convert.ToDouble(part.CenterX) * Constants.MIL_TO_MM);
            double part_y = Board.PcbOriginY + (Convert.ToDouble(part.CenterY) * Constants.MIL_TO_MM);
            Messages.Add(GCommand.G_SetXYPosition(part_x, part_y));
            Messages.Add(GCommand.OffsetCameraToPick(part));
            Messages.Add(GCommand.G_SetXYPosition(0, 0));
            Messages.Add(GCommand.G_SetRotation(Convert.ToDouble(part.Rotation)));
            Messages.Add(GCommand.G_FinishMoves());
            Messages.Add(GCommand.G_ProbeZ(Constants.PCB_NOMINAL_Z_DRIVE_MM));
            Messages.Add(GCommand.G_FinishMoves());
            Messages.Add(GCommand.G_EnablePump(false));
            Messages.Add(GCommand.G_EnableValve(false));
            Messages.Add(GCommand.Delay(10));
            Messages.Add(GCommand.G_SetZPosition(0));
            return true;
        }

        public bool AddPartLocationToQueue(Part part)
        { /* Sets OPTICAL LOCATION */
            double part_x = Board.PcbOriginX + (Convert.ToDouble(part.CenterX) * Constants.MIL_TO_MM);
            double part_y = Board.PcbOriginY + (Convert.ToDouble(part.CenterY) * Constants.MIL_TO_MM);
            Messages.Add(GCommand.G_SetXYPosition(part_x, part_y));
            //Messages.Add(GCommand.G_SetRotation(Convert.ToDouble(part.Rotation)));
            Messages.Add(GCommand.G_FinishMoves());
            //Messages.Add(GCommand.G_ProbeZ(Constants.PCB_NOMINAL_Z_DRIVE_MM));
            Messages.Add(GCommand.G_FinishMoves());
            //Messages.Add(GCommand.G_EnablePump(false));
            //Messages.Add(GCommand.G_EnableValve(false));
            Messages.Add(GCommand.Delay(10));
            Messages.Add(GCommand.G_SetZPosition(0));
            return true;
        }
    }
}
