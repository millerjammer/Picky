using OpenCvSharp;
using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Windows.Controls.Primitives;
using OpenCvSharp.Flann;
using System.Runtime.InteropServices;
using System.CodeDom;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

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
        public double Length { get; set; }
        public double Diameter { get; set; }
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
        
        private TipStates tipState;
        public TipStates TipState
        {
            get { return tipState; }
            set { tipState = value; OnPropertyChanged(nameof(TipState)); }
        }

        /* Create the data point used for tip calibration */
        public List<Polar> CalDataPoints = new List<Polar>();
        
        private Polar tipOffsetUpper;
        public Polar TipOffsetUpper
        {
            get { return tipOffsetUpper; }
            set { tipOffsetUpper = value; OnPropertyChanged(nameof(TipOffsetUpper)); }
        }

        private Polar tipOffsetLower;
        public Polar TipOffsetLower
        {
            get { LoadCalibrationCircleFromFile(); return tipOffsetLower; }
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
            TipOffsetUpper = new Polar(0, 0, 0);
            TipOffsetLower = new Polar(0, 0, 0);
            TipState = TipStates.Unknown;

            TipList = new List<TipStyle>
            {
                new TipStyle("28GA <Fine>", 1.0),
                new TipStyle("24GA <Small>", 1.5),
                new TipStyle("20GA <Medium>", 2.0),
                new TipStyle("18GA <Large>", 2.5),
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

        public bool SetPickOffsetCalibrationData(Polar point)
        {
            /* Calculate PickOffset from Calibration Data */
            CalDataPoints.Add(point);
            if(CalDataPoints.Count >= 12) {
                List<Polar> lower = CalDataPoints.Where(e => e.z == Constants.TOOL_LOWER_Z_CAL).ToList();
                List<Polar> upper = CalDataPoints.Where(e => e.z == Constants.TOOL_UPPER_Z_CAL).ToList();

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
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), (UniqueID + "_tipCal.jpg"));
            Mat CalMat = new Mat();
            CalMat = Cv2.ImRead(path);
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
            MachineModel machine = MachineModel.Instance;
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), (Constants.CAMERA_FRAME_HEIGHT / 3), Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            Mat roiImage = new Mat(machine.upCamera.ColorImage, roi);
            Mat CalMat = roiImage.Clone();
            var scale = machine.Cal.GetScaleMMPerPixAtZ(25);
            for (int i = 0; i < 12; i++)
            {
                // Draw the circle outline
                Polar item = CalDataPoints.ElementAt(i);
                x = (int)((CalMat.Width / 2) + (item.x / scale.xScale));
                y = (int)((CalMat.Height / 2) + (item.y / scale.yScale));
                r = (int)(item.radius / scale.xScale);
                // Draw the circle center
                if(item.z == 0)
                    Cv2.Circle(CalMat, x, y, 3, Scalar.Red, 3);
                else
                    Cv2.Circle(CalMat, x, y, 3, Scalar.Green, 3);
            }
            // Draw Result Calibration Upper and Lower
            x = (int)((CalMat.Width / 2) + (TipOffsetUpper.x / scale.xScale));
            y = (int)((CalMat.Height / 2) + (TipOffsetUpper.y / scale.yScale));
            r = (int)(TipOffsetUpper.radius / scale.xScale);
            Cv2.Circle(CalMat, x, y, r, Scalar.Black, 1);
            // Draw the circle center - Upper
            Cv2.Circle(CalMat, x, y, 3, Scalar.Black, 3);
            x = (int)((CalMat.Width / 2) + (TipOffsetLower.x / scale.xScale));
            y = (int)((CalMat.Height / 2) + (TipOffsetLower.y / scale.yScale));
            r = (int)(TipOffsetLower.radius / scale.xScale);
            Cv2.Circle(CalMat, x, y, r, Scalar.White, 1);
            // Draw the circle center - Lower
            Cv2.Circle(CalMat, x, y, 3, Scalar.White, 3);
                        
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
