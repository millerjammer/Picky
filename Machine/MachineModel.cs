using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using static System.Windows.Forms.AxHost;

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

        /* SettingsUpper */
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
        private Cassette selectedCassette;
        public Cassette SelectedCassette
        {
            get { return selectedCassette; }
            set { selectedCassette = value; OnPropertyChanged(nameof(SelectedCassette)); }
        }
        public ObservableCollection<Cassette> Cassettes { get; set; }
                
        /* Current machine position - needed because serial port makes changes here */
        private Position3D current = new Position3D();
        public Position3D Current
        {
            get { return current; }
            set { current = value; OnPropertyChanged(nameof(Current)); }
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
        private double currentP = 0;
        public double CurrentP
        {
            get { return currentP; }
            set { currentP = value; OnPropertyChanged(nameof(CurrentP)); }
        }

        public enum PickHeadRegion { Unknown, FeederPick, FeederQR, Deck, PCBPlacement, ToolStorage, UpperCalPad, DeckCalPad, Error }
        private PickHeadRegion region = PickHeadRegion.Unknown;
        public PickHeadRegion Region
        {
            get { return region; }
            set { region = value; OnPropertyChanged(nameof(Region)); } //Notify listeners
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

        private bool isMachineInMotion = false;
        public bool IsMachineInMotion
        {
            get { return isMachineInMotion; }
            set { isMachineInMotion = value; OnPropertyChanged(nameof(IsMachineInMotion)); }
        }

        /* Limit Switches */
        private bool isZProbeAtLimit = false;
        public bool IsZProbeAtLimit
        {
            get { return isZProbeAtLimit; }
            set { isZProbeAtLimit = value; OnPropertyChanged(nameof(IsZProbeAtLimit)); }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("FeederModel collection changed");
            OnPropertyChanged(nameof(Cassettes)); // Notify that the collection has changed
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FeederModel GetClosestFeeder()
        {
            /*------------------------------------------------------------------------
             * Not really a use for this but, whatever
             * 
             * ----------------------------------------------------------------------*/
            
            if (Cassettes.Count > 0 && Cassettes.ElementAt(0).Feeders != null)
                return Cassettes.ElementAt(0).Feeders.OrderBy(feeder => TranslationUtils.GetDistance(feeder.Origin, Current)).FirstOrDefault();
            else
                return null;
        }

        private void UpdateCurrentRegion(object sender, PropertyChangedEventArgs e)
        {
            /*------------------------------------------------------------------------
             * This function updates properties in various objects based on movement
             * Movements set the camera, but only when the Region changes
             * ----------------------------------------------------------------------*/

            if (sender.Equals(Current))
            {
                /* Get QR Region */
                if (Current.Y >= Cal?.QRRegion.Y)
                {
                    if ((Current.Y < (Cal?.QRRegion.Y + Cal?.QRRegion.Height)) && Region != PickHeadRegion.FeederQR)
                    {
                        Region = PickHeadRegion.FeederQR;
                        /* We don't set the camera QR functions are triggered by commands and they will set camera settings */
                    }
                    else if(Current.Y >= Cal?.ChannelRegion.Y && Region != PickHeadRegion.FeederPick)
                    {
                        Region = PickHeadRegion.FeederPick;
                        FeederModel feeder = GetClosestFeeder();
                        if(feeder != null)
                            downCamera.Settings = feeder.CaptureSettings.Clone();
                    }
                }
                else if(Region != PickHeadRegion.Deck)
                {
                    Region = PickHeadRegion.Deck;
                }
            }
        }
        private MachineModel()
        {
            Messages = new ObservableCollection<MachineMessage>();
            Cassettes = new ObservableCollection<Cassette>();
            PickList = new ObservableCollection<Part>();

            current.PropertyChanged += UpdateCurrentRegion;

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
                Console.WriteLine("ERROR: " + ex.ToString() + " Loading File: " + path + "\\" + Constants.TOOL_FILE_NAME);
                PickToolList = new ObservableCollection<PickToolModel>();
            }
            foreach (var item in PickToolList)
            {
                item.State = PickToolModel.TipStates.Unknown;
            }
        }

        public static MachineModel Instance
        {
            get { return lazy.Value; }
        }
        
        public void SaveSettings()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filename = path + "\\" + Constants.SETTINGS_FILE_NAME;
            File.WriteAllText(filename, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            string msg = string.Format("Settings Successfully.\n{0}", filename);
            ConfirmationDialog dlg = new ConfirmationDialog(msg);
            dlg.ShowDialog();
        }

        public void SaveTools()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filename = path + "\\" + Constants.TOOL_FILE_NAME;
            File.WriteAllText(filename, JsonConvert.SerializeObject(PickToolList, Formatting.Indented));
            string msg = string.Format("Tools Configuration Successfully.\n{0}", filename);
            ConfirmationDialog dlg = new ConfirmationDialog(msg);
            dlg.ShowDialog();
        }

        public void SaveCalibration()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filename = path + "\\" + Constants.CALIBRATION_FILE_NAME;
            File.WriteAllText(filename, JsonConvert.SerializeObject(Cal, Formatting.Indented));
            string msg = string.Format("Calibration Configuration Successfully.\n{0}", filename);
            ConfirmationDialog dlg = new ConfirmationDialog(msg);
            dlg.ShowDialog();
        }

        public ICommand SaveFeederCommand { get { return new RelayCommand(SaveFeeder); } }
        public void SaveFeeder()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FeederModel feeder = SelectedCassette.SelectedFeeder;
            String filename = FileUtils.ConvertToHashedFilename(feeder.QRCode, Constants.FEEDER_FILE_EXTENTION);
            File.WriteAllText(path + "\\" + filename, JsonConvert.SerializeObject(feeder, Formatting.Indented));
            string msg = string.Format("Feeder Saved Successfully.\n{0}", filename);
            ConfirmationDialog dlg = new ConfirmationDialog(msg);
            dlg.ShowDialog();
        }
                
        public bool AddFeederPickToQueue(FeederModel feeder)
        {
            MachineModel machine = MachineModel.Instance;
            //Go to FeederModel Origin
            Messages.Add(GCommand.SetCameraManualFocus(downCamera, true, Constants.FOCUS_FEEDER_QR_CODE));
            Messages.Add(GCommand.G_SetPosition(feeder.Origin.X, feeder.Origin.Y, 0, 0, 0));
            Messages.Add(GCommand.G_FinishMoves());
            Messages.Add(GCommand.SetCameraManualFocus(downCamera, true, Constants.FOCUS_FEEDER_PART));
            //Messages.Add(GCommand.OpticallyAlignToPart(feeder));
            Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            //Messages.Add(GCommand.OffsetCameraToPick(FeederModel.Part, machine.selectedPickTool.TipOffsetLower.BestCircle.Z + 2.0));
            Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            Messages.Add(GCommand.G_ProbeZ(Constants.PART_NOMINAL_Z_DRIVE_MM));
            Messages.Add(GCommand.G_FinishMoves());
            //Messages.Add(GCommand.G_EnablePump(true));
            //Messages.Add(GCommand.G_EnableValve(false));
            Messages.Add(GCommand.Delay(100));
            Messages.Add(GCommand.G_SetZPosition(0));
            Messages.Add(GCommand.G_FinishMoves());
            return true;
        }

        public bool AddPartPlacementToQueue(Part part)
        {
            MachineModel machine = MachineModel.Instance;
            /* Sets PLACE location */
            double part_x = Board.PcbOriginX + (Convert.ToDouble(part.CenterX) * Constants.MIL_TO_MM);
            double part_y = Board.PcbOriginY + (Convert.ToDouble(part.CenterY) * Constants.MIL_TO_MM);
            Messages.Add(GCommand.G_SetXYPosition(part_x, part_y));
            //Messages.Add(GCommand.OffsetCameraToPick(Part, machine.selectedPickTool.TipOffsetLower.BestCircle.Z - 2.0));
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
    }
}
