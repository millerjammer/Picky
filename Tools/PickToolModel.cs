using OpenCvSharp;
using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Input;

namespace Picky
{ 
     public class TipStyle
    {
        public string TipName { get; set; }
        public double TipDia  { get; set; }
        public TipStyle(string name, double diameter_mm)
        {
            TipName = name;
            TipDia = diameter_mm;
        }
    }

    public class PickToolModel : INotifyPropertyChanged
    {
        

        public enum TipStates { Unknown, Loading, Calibrating, Ready, Unloading, Stored, Error }
        
        /* Measured Properties */
        public string Description { get; set; }
        public string UniqueID { get; set; }

        public Point2d ToolReturnLocation { get; set; }

        public List<TipStyle> TipList { get; set; }

        private TipStyle selectedTip;
        public TipStyle SelectedTip
        {
            get { return selectedTip; }
            set { selectedTip = value; OnPropertyChanged(nameof(SelectedTip)); } //Notify listeners
        }

        [JsonIgnore] // This property will not be serialized
        private BitmapSource tipCalImage;
        [JsonIgnore] // This property will not be serialized
        public BitmapSource TipCalImage
        {
            get { return tipCalImage; }
            set { tipCalImage = value;  OnPropertyChanged(nameof(TipCalImage)); }
        }

        private double toolStorageX;
        public double ToolStorageX
        {
            get { return toolStorageX; }
            set { toolStorageX = value; OnPropertyChanged(nameof(ToolStorageX)); }
        }

        private double toolStorageY;
        public double ToolStorageY
        {
            get { return toolStorageY; }
            set { toolStorageY = value; OnPropertyChanged(nameof(ToolStorageY)); }
        }

        private double toolStorageZ;
        public double ToolStorageZ
        {
            get { return toolStorageZ; }
            set { toolStorageZ = value; OnPropertyChanged(nameof(ToolStorageZ)); }
        }

        private double length;
        public double Length
        {
            get { return length; }
            set { length = value; OnPropertyChanged(nameof(Length)); }
        }

        private CircleDetector upperCircleDetector = new CircleDetector();
        public CircleDetector UpperCircleDetector
        {
            get { return upperCircleDetector; }
            set { upperCircleDetector = value; OnPropertyChanged(nameof(UpperCircleDetector)); }
        }

        private CircleDetector lowerCircleDetector = new CircleDetector();
        public CircleDetector LowerCircleDetector
        {
            get { return lowerCircleDetector; }
            set { lowerCircleDetector = value; OnPropertyChanged(nameof(LowerCircleDetector)); }
        }

        private TipStates tipState;
        public TipStates TipState
        {
            get { return tipState; }
            set { tipState = value; OnPropertyChanged(nameof(TipState)); }
        }

        /* Create the data point used for tip calibration */
        public List<Position3D> CalDataPoints = new List<Position3D>();
        
        private Position3D tipOffsetUpper;
        public Position3D TipOffsetUpper
        {
            get { return tipOffsetUpper; }
            set { tipOffsetUpper = value; OnPropertyChanged(nameof(TipOffsetUpper)); }
        }

        private Position3D tipOffsetLower;
        public Position3D TipOffsetLower
        {
            get { return tipOffsetLower; }
            set { tipOffsetLower = value; OnPropertyChanged(nameof(TipOffsetLower)); }
        }
        
        public PickToolModel(string name)
        {
            Description = name;
            /* If name is null - we're being deserialized - don't update UniqueID. 
               Deserialization seems to run the construct and update fields that differ 
               documentation doesn't indicate that's what it does, but this workaround
               works.  Note that the calibration file is only loaded when tipOffsetLower 
               changes TODO - fix */
            if (name != null)
                UniqueID = DateTime.Now.ToString("HHmmss-dd-MM-yy");
            TipOffsetUpper = new Position3D(0, 0, 0);
            TipOffsetLower = new Position3D(0, 0, 0);
            TipState = TipStates.Unknown;
                        
            TipList = new List<TipStyle>
            {
                new TipStyle("28GA <Fine>", 1.2),
                new TipStyle("24GA <Small>", 1.5),
                new TipStyle("20GA <Medium>", 2.2),
                new TipStyle("18GA <Large>", 3.5),
            };
            SelectedTip = TipList.FirstOrDefault();
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool ResetPickOffsetCalibrationData()
        {
            CalDataPoints.Clear();
            return true;
        }
        [JsonIgnore] // This property will not be serialized
        public ICommand GetUpperCircleDetectorCommand { get { return new RelayCommand(GetUpperCircleDetector); } }
        private void GetUpperCircleDetector()
        {
            MachineModel machine = MachineModel.Instance;
            UpperCircleDetector.Threshold = machine.downCamera.BinaryThreshold;
            UpperCircleDetector.Param1 = machine.downCamera.CircleDetectorP1;
            UpperCircleDetector.Param2 = machine.downCamera.CircleDetectorP2;
            UpperCircleDetector.Focus = machine.downCamera.Focus;
        }
        [JsonIgnore] // This property will not be serialized
        public ICommand SetUpperCircleDetectorCommand { get { return new RelayCommand(SetUpperCircleDetector); } }
        private void SetUpperCircleDetector()
        {
            MachineModel machine = MachineModel.Instance;
            machine.downCamera.BinaryThreshold = UpperCircleDetector.Threshold;
            machine.downCamera.CircleDetectorP1 = UpperCircleDetector.Param1;
            machine.downCamera.CircleDetectorP2 = UpperCircleDetector.Param2;
            machine.downCamera.Focus = UpperCircleDetector.Focus;
        }
        [JsonIgnore] // This property will not be serialized
        public ICommand GetLowerCircleDetectorCommand { get { return new RelayCommand(GetLowerCircleDetector); } }
        private void GetLowerCircleDetector()
        {
            MachineModel machine = MachineModel.Instance;
            LowerCircleDetector.Threshold = machine.downCamera.BinaryThreshold;
            LowerCircleDetector.Param1 = machine.downCamera.CircleDetectorP1;
            LowerCircleDetector.Param2 = machine.downCamera.CircleDetectorP2;
            LowerCircleDetector.Focus = machine.downCamera.Focus;
        }
        [JsonIgnore] // This property will not be serialized
        public ICommand SetLowerCircleDetectorCommand { get { return new RelayCommand(SetLowerCircleDetector); } }
        private void SetLowerCircleDetector()
        {
            MachineModel machine = MachineModel.Instance;
            machine.downCamera.BinaryThreshold = LowerCircleDetector.Threshold;
            machine.downCamera.CircleDetectorP1 = LowerCircleDetector.Param1;
            machine.downCamera.CircleDetectorP2 = LowerCircleDetector.Param2;
            machine.downCamera.Focus = LowerCircleDetector.Focus;
        }

        public bool SetPickOffsetCalibrationData(Position3D point)
        {
            /* Calculate PickOffset from Calibration Data */
            CalDataPoints.Add(point);
            if(CalDataPoints.Count >= 12) {
                double maxZ = CalDataPoints.Max(e => e.Z);
                double minZ = CalDataPoints.Min(e => e.Z);
                List<Position3D> lower = CalDataPoints.Where(e => e.Z == maxZ).ToList();
                List<Position3D> upper = CalDataPoints.Where(e => e.Z == minZ).ToList();

                TipFitCalibration fitter = new TipFitCalibration();
                TipOffsetLower = fitter.CalculateBestFitCircle(lower);
                TipOffsetUpper = fitter.CalculateBestFitCircle(upper);
                TipState = TipStates.Ready;
                SaveCalibrationCircleToFile();
            }
            return true;
        }

        public void LoadCalibrationCircleFromFile()
        {
            Console.WriteLine("Loading Calibration Circle from File.");
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), (UniqueID + "_tipCal.jpg"));
            Mat CalMat = new Mat();
            try
            {
                CalMat = Cv2.ImRead(path);
            }
            catch { Console.WriteLine("No file found."); }
            if (CalMat.Empty()) { 
                CalMat = Cv2.ImRead("no-image.jpg");
            }
            Console.WriteLine("Loading Tip Calibration Image: " + path);
            if (CalMat.Width > 0 && CalMat.Height > 0)
                TipCalImage = BitmapSource.Create(CalMat.Width, CalMat.Height, 96, 96, PixelFormats.Bgr24, null, CalMat.Data, (int)(CalMat.Step() * CalMat.Height), (int)CalMat.Step());

        }

        public void SaveCalibrationCircleToFile()
        {
            int x, y, r;
            Console.WriteLine("Saving Calibration Circle to File.");
            MachineModel machine = MachineModel.Instance;
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), 0, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 4);
            Mat roiImage = new Mat(machine.downCamera.ColorImage, roi);
            Mat CalMat = roiImage.Clone();
            for (int i = 0; i < 12; i++)
            {
                // Draw the circle outline
                Position3D item = CalDataPoints.ElementAt(i);
                x = (int)((CalMat.Width / 2) + (item.X));
                y = (int)((CalMat.Height / 2) + (item.Y));
                r = (int)(item.Radius);
                // Draw the circle center
                if(item.Z == CalDataPoints.Min(e => e.Z))
                    Cv2.Circle(CalMat, x, y, 3, Scalar.Red, 3);
                else
                    Cv2.Circle(CalMat, x, y, 3, Scalar.Green, 3);
            }
            // Draw Result Calibration Upper and Lower
            x = (int)((CalMat.Width / 2) + (TipOffsetUpper.X));
            y = (int)((CalMat.Height / 2) + (TipOffsetUpper.Y));
            r = (int)(TipOffsetUpper.Radius);
            Cv2.Circle(CalMat, x, y, r, Scalar.Black, 1);
            // Draw the circle center - Upper
            Cv2.Circle(CalMat, x, y, 3, Scalar.Black, 3);
            x = (int)((CalMat.Width / 2) + (TipOffsetLower.X));
            y = (int)((CalMat.Height / 2) + (TipOffsetLower.Y));
            r = (int)(TipOffsetLower.Radius);
            Cv2.Circle(CalMat, x, y, r, Scalar.Black, 1);
            // Draw the circle center - Lower
            Cv2.Circle(CalMat, x, y, 3, Scalar.Black, 3);
                        
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), (UniqueID + "_tipCal.jpg"));
            Cv2.ImWrite(path, CalMat);
            /* Update Bitmap in the UI thread */
            Application.Current.Dispatcher.Invoke(() =>
            {
                TipCalImage = BitmapSource.Create(CalMat.Width, CalMat.Height, 96, 96, PixelFormats.Bgr24, null, CalMat.Data, (int)(CalMat.Step() * CalMat.Height), (int)CalMat.Step());
                machine.SaveTools();
            });
            Console.WriteLine("Tip Calibration Image Saved: " + path);
        }
    }
}
