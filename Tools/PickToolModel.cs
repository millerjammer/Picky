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
using System.Security.Permissions;
using OpenCvSharp.Dnn;
using EnvDTE90;

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
        public List<TipStyle> TipList { get; set; }

        private TipStyle selectedTip;
        public TipStyle SelectedTip
        {
            get { return selectedTip; }
            set { selectedTip = value; OnPropertyChanged(nameof(SelectedTip)); } //Notify listeners
        }

        private TipStates state;
        public TipStates State
        {
            get { return state; }
            set { state = value; OnPropertyChanged(nameof(State)); } //Notify listeners
        }

        public string Description { get; set; }
        public string UniqueID { get; set; }

        /* Default search are for tool calbration */
        private OpenCvSharp.Rect toolROI { get; set; } 
                
        private string toolTemplateFileName;
        public string ToolTemplateFileName
        {
            get { return toolTemplateFileName; }
            set { toolTemplateFileName = value; OnPropertyChanged(nameof(ToolTemplateFileName)); }
        }
        
        private Position3D toolStorage;
        public Position3D ToolStorage
        {
            get { return toolStorage; }
            set { toolStorage = value; OnPropertyChanged(nameof(ToolStorage)); }
        }

        private double length;
        public double Length
        {
            get { return length; }
            set { if (length != value) { length = value; OnPropertyChanged(nameof(Length)); } }
        }

        private CameraSettings settings;
        public CameraSettings Settings
        {
            get { return settings; }
            set { settings = value; OnPropertyChanged(nameof(Settings)); }
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
            toolStorage = new Position3D();
            state = TipStates.Unknown;
                        
            TipList = new List<TipStyle>
            {
                new TipStyle("28GA <Fine>", 1.2),
                new TipStyle("24GA <Small>", 1.5),
                new TipStyle("20GA <Medium>", 2.2),
                new TipStyle("18GA <Large>", 3.5),
            };
            SelectedTip = TipList.FirstOrDefault();
            toolROI = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), 0, Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 5);
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
                
        public void SetToolTemplate()
        {
            /*--------------------------------------------------------------
             * Called by GUI to save template for use later in tip guidance.
             * Does not save storage location or location of calibration.
             * ------------------------------------------------------------*/
                       
            MachineModel machine = MachineModel.Instance;
            //ToolThreshold = machine.downCamera.TemplateThreshold;
            //ToolFocus = Constants.CAMERA_AUTOFOCUS;
            //if (machine.downCamera.IsManualFocus)
            //    ToolFocus = machine.downCamera.Focus;
            machine.downCamera.Settings = Settings.Clone();
            Mat template = new Mat(machine.downCamera.ColorImage, toolROI);

            // Write part template to file
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ToolTemplateFileName = path + "\\toolTemplate-" + UniqueID + ".png";
            Cv2.ImWrite(ToolTemplateFileName, template);
            while (File.Open(ToolTemplateFileName, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }

            Console.WriteLine("Set Tool Target Template: " + ToolTemplateFileName);
        }

        public void PreviewToolTemplate(Position3D pos)
        {
            /*--------------------------------------------------------------
             * Called by GUI to kickoff a preview session that occurs at the
             * calibration deck - 2mm. Will terminate automatically by the GUI
             * ------------------------------------------------------------*/
            MachineModel machine = MachineModel.Instance;
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(pos.X, pos.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetZPosition(-2.0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            machine.Messages.Add(GCommand.G_FinishMoves());

            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
            Mat template = Cv2.ImRead(ToolTemplateFileName, ImreadModes.Color);
            machine.downCamera.RequestTemplateSearch(template, roi, Settings);
        }


        public (double x, double y) GetTipOffset(Mat image) {
        /****************************************************************************
         * Returns the position of the tip in an image, typically a full image.  
         * Units are in pixels.
         * **************************************************************************/
                  
            return (0, 0);
        }
        
        public void LoadCalibrationCircleFromFile()
        {
            /*Console.WriteLine("Loading Calibration Circle from File.");
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), (UniqueID + "_tipCal.jpg"));
            CalMat = new Mat();
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
            */
        }
    }
}
