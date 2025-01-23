using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Picky.Tools;
using Xamarin.Forms.Xaml;

namespace Picky
{
    public class TipStyle
    {
        public string TipName { get; set; }
        public double TipDia { get; set; }
        public TipStyle(string name, double diameter_mm)
        {
            TipName = name;
            TipDia = diameter_mm;
        }
    }

    public class PickToolModel : INotifyPropertyChanged
    {
        public enum TipStates { Unknown, Loading, Calibrating, Ready, Unloading, Stored, Error }
        private TipStates state;
        public TipStates State
        {
            get { return state; }
            set { state = value; OnPropertyChanged(nameof(State)); } //Notify listeners
        }

        public List<TipStyle> TipList { get; set; }
        private TipStyle selectedTip;
        public TipStyle SelectedTip
        {
            get { return selectedTip; }
            set { selectedTip = value; OnPropertyChanged(nameof(SelectedTip)); } //Notify listeners
        }

        public string Description { get; set; }
        public string UniqueID { get; set; }

        /* This is the area where the tool is expect to be found */
        private OpenCvSharp.Rect toolSearchROI;
        public OpenCvSharp.Rect ToolSearchROI
        {
            get { return toolSearchROI; }
            set { toolSearchROI = value; OnPropertyChanged(nameof(ToolSearchROI)); }
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

        private PickToolCalPosition upperCal;
        public PickToolCalPosition UpperCal
        {
            get { return upperCal; }
            set { upperCal = value; OnPropertyChanged(nameof(UpperCal)); }
        }

        private PickToolCalPosition lowerCal;
        public PickToolCalPosition LowerCal
        {
            get { return lowerCal; }
            set { lowerCal = value; State = TipStates.Ready; OnPropertyChanged(nameof(LowerCal)); }
        }
             

        public PickToolModel(string name)
        {
            Description = name;
            
            /* If name is null - we're being deserialized - don't update UniqueID. */
            if (name != null)
            {
                UniqueID = DateTime.Now.ToString("HHmmss-dd-MM-yy");
            }
            if (LowerCal == null || UpperCal == null)
            {
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                LowerCal = new PickToolCalPosition(Constants.LOWER_FOCUS, Constants.LOWER_THRESHOLD);
                UpperCal = new PickToolCalPosition(Constants.UPPER_FOCUS, Constants.UPPER_THRESHOLD);
                LowerCal.ToolTemplateFileName = path + "\\toolTemplate-lower-" + UniqueID + ".png";
                UpperCal.ToolTemplateFileName = path + "\\toolTemplate-upper-" + UniqueID + ".png";
            }
            ToolStorage = new Position3D();
            state = TipStates.Unknown;

            TipList = new List<TipStyle>
            {
                new TipStyle("28GA <Fine>", 1.2),
                new TipStyle("24GA <Small>", 1.5),
                new TipStyle("20GA <Medium>", 2.2),
                new TipStyle("18GA <Large>", 3.5),
            };
            SelectedTip = TipList.FirstOrDefault();

            double width = (Constants.CAMERA_FRAME_WIDTH / 4);
            double height = (Constants.CAMERA_FRAME_HEIGHT / 6);
            double x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            double y = 0;
            LowerCal.toolROI = new OpenCvSharp.Rect((int)x, (int)y, (int)width, (int)height);
            UpperCal.toolROI = new OpenCvSharp.Rect((int)x, (int)y, (int)width, (int)height);

            width *= 1.2;
            height = Constants.CAMERA_FRAME_HEIGHT;
            x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            ToolSearchROI = new OpenCvSharp.Rect((int)x, (int)y, (int)width, (int)height);
         
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadImagery()
        {
            LowerCal.LoadToolTemplateImage(); 
            UpperCal.LoadToolTemplateImage();
        }
        
        public void SaveCalPosition(PickToolCalPosition calPosition)
        {   
            // Get Settings
            MachineModel machine = MachineModel.Instance;
            calPosition.CaptureSettings = machine.downCamera.Settings.Clone();

            // Write Part template image to file
            calPosition.SaveToolTemplateImage();
            
            calPosition.Set3DToolTipFromToolMat(machine.downCamera.DilatedImage, machine.Current.Z);
            Console.WriteLine("Set Tool Target Template: " + calPosition.ToolTemplateFileName);
        }

        public void CalibrateTool()
        {
            /*--------------------------------------------------------------
             * Called by GUI to calibrate this tool.  Must have previously
             * set the CameraSettings, and Set CalDeck and CalPad locations.
             * Will update the tip position (x, y and z (in mm)) for use with
             * accurate picking commands.
             * ------------------------------------------------------------*/

            MachineModel machine = MachineModel.Instance;
            double offset_to_head_x = Constants.CAMERA_TO_HEAD_OFFSET_X_MM;
            double offset_to_head_y = Constants.CAMERA_TO_HEAD_OFFSET_Y_MM;
            State = TipStates.Calibrating;
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X + offset_to_head_x, machine.Cal.CalPad.Y + offset_to_head_y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.SetToolCalibration(UpperCal));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.DeckPad.X + offset_to_head_x, machine.Cal.DeckPad.Y + offset_to_head_y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.SetToolCalibration(LowerCal));
            // Raise Probe
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.DeckPad.X, machine.Cal.DeckPad.Y, 0, 0, 0));
        }      

        public void PreviewToolTemplate(Position3D pos)
        {
            /*--------------------------------------------------------------
             * Called by GUI to kickoff a preview session that occurs at the
             * calibration deck - 2mm. Will terminate automatically by the GUI
             * ------------------------------------------------------------*/

            PickToolCalPosition calPosition = LowerCal;
            MachineModel machine = MachineModel.Instance;
            if (pos == machine.Cal.CalPad)
                calPosition = UpperCal;

            if (calPosition.ToolTemplateImage == null)
            { //If there's no template, create anything
                calPosition.CaptureSettings  = machine.downCamera.Settings.Clone();
            }

            double offset_to_head_x = Constants.CAMERA_TO_HEAD_OFFSET_X_MM;
            double offset_to_head_y = Constants.CAMERA_TO_HEAD_OFFSET_Y_MM;

            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.SetCamera(calPosition.CaptureSettings, machine.downCamera));
            machine.Messages.Add(GCommand.G_SetPosition(pos.X + offset_to_head_x, pos.Y + offset_to_head_y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.SetToolCalibration(calPosition));
            machine.Messages.Add(GCommand.G_FinishMoves());

            Mat ToolTemplate = Cv2.ImRead(calPosition.ToolTemplateFileName, ImreadModes.Color);
            machine.downCamera.RequestTemplateSearch(ToolTemplate, ToolSearchROI);
        }

        public Position3D GetToolTipOffsetAtZ(double z) 
        {
            /*----------------------------------------------------
             * Given a z in MM this function returns the x, y, z
             * position of the tip based on calibration and 
             * similar triangles.  This is focus corrected.  There
             * is a MANUAL offset in y equal to tip radius / 2
             * --------------------------------------------------*/

            double zUpper = UpperCal.TipOffsetMM.Z;
            double zLower = LowerCal.TipOffsetMM.Z;
            if (zUpper == zLower )
                throw new ArgumentException("zUpper and zLower must be different values.");
            
            // Calculate the scaling factor based on similar triangles
            double scaleFactor = (z - zLower) / (zUpper - zLower);

            // Interpolate the X and Y positions
            double rad = LowerCal.TipOffsetMM.Radius + (scaleFactor * (UpperCal.TipOffsetMM.Radius - LowerCal.TipOffsetMM.Radius));
            double iX = LowerCal.TipOffsetMM.X + (scaleFactor * (UpperCal.TipOffsetMM.X - LowerCal.TipOffsetMM.X));
            double iY = LowerCal.TipOffsetMM.Y + (scaleFactor * (UpperCal.TipOffsetMM.Y - LowerCal.TipOffsetMM.Y)) + (rad/4);
            
            Position3D pos = new Position3D(iX, iY, z, 0);
            pos.Radius = rad;
            return (pos);
        }
    }
}
