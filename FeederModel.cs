﻿using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using System.Windows;

namespace Picky
{
    public class FeederModel : INotifyPropertyChanged
    {
        /* Calibration GridOrigin Size and Location of 1st */
        public static double FEEDER_DEFAULT_WIDTH_MM = 12.5;
        public static double FEEDER_DEFAULT_PART_OFFSET = 3.5;
        public static double FEEDER_DEFAULT_INTERVAL_MM = 5;
        public static double MACHINE_DRIVELINE_TO_FEEDER_DRIVE_YOFFSET_MM = 0;
        public static double FEEDER_ORIGIN_TO_DRIVE_XOFFSET_MM = 0;
        public static double PICK_TERMINAL_TRAVEL = 3.0;
        public static double PICK_CHANNEL_HEIGHT = 40;
        public static double DRIVE_APPROACH_FEED_FACTOR = 0.25;
        /* The motor controller runs the motor for a specific time as it doesn't
         * have feedback.  So, adjust this so that when advance is called you get 
         * something close */
        public static double DISTANCE_TO_TRAVEL_TIME = 13;


        MachineModel machine = MachineModel.Instance;

        private Part part;
        public Part Part
        {
            get { return part; }
            set { part = value; OnPropertyChanged(nameof(Part)); }
        }

        private double interval = FEEDER_DEFAULT_INTERVAL_MM;
        public double Interval
        {
            get { return interval; }
            set { interval = value; OnPropertyChanged(nameof(Interval)); }
        }

        private double width = FEEDER_DEFAULT_WIDTH_MM;
        public double Width
        {
            get { return width;  }
            set { width = value; OnPropertyChanged(nameof(Width)); }
        }

        private double partOffset = FEEDER_DEFAULT_PART_OFFSET;
        public double PartOffset
        {
            get { return partOffset; }
            set { partOffset = value; OnPropertyChanged(nameof(PartOffset)); }
        }

        public int Index { get; set; }
        public int start_count { get; set; }
        public int placed_count { get; set; }
        public double thickness { get; set; }
        

        private Position3D nextPartOpticalLocation = new Position3D(0, 0, 0);
        public Position3D NextPartOpticalLocation
        {
            get { return nextPartOpticalLocation; }
            set { if (nextPartOpticalLocation != value) { nextPartOpticalLocation = value; updatePartPickLocation(); OnPropertyChanged(nameof(NextPartOpticalLocation)); }  }
        }

        public Position3D nextPartPickLocation = new Position3D(0, 0);
        public Position3D NextPartPickLocation
        {
            get { return nextPartPickLocation; }
            set { nextPartOpticalLocation = value; OnPropertyChanged(nameof(NextPartPickLocation)); }
        }
        
        private Position3D origin = new Position3D(0, 0, 0);
        public Position3D Origin
        {
            get { return origin; }
            set { origin = value; OnPropertyChanged(nameof(Origin)); }
        }

        private string qrCode;
        public string QRCode
        {
            get { return qrCode; }
            set { qrCode = value; OnPropertyChanged(nameof(QRCode)); }
        }
        
        public Point2d QRLocation { get; set; }

        /* Drive offset.Y is in reference to Machine.DriveLineY */
        private Position3D driveOffset = new Position3D(0, 0);
        public Position3D DriveOffset
        {
            get { return driveOffset; }
            set { driveOffset = value; OnPropertyChanged(nameof(DriveOffset)); }
        }


        private PickToolModel pickTool;
        public PickToolModel PickTool
        {
            get { return pickTool; }
            set { pickTool = value; OnPropertyChanged(nameof(PickTool)); }
        }

        private CameraSettings captureSettings = new CameraSettings();
        public CameraSettings CaptureSettings
        {
            get { return captureSettings; }
            set { captureSettings = value; OnPropertyChanged(nameof(captureSettings)); }
        }

        public FeederModel()
        {
            part = new Part();
            part.PropertyChanged += OnPartPropertyChanged;
        }

        public Position3D GetPickROI()
        {
            /*----------------------------------------------------------
             * Return the ROI for picking parts from this feeder in 
             * terms of mm referenced to the global frame. Returned ROI
             * is in mm.  The Pick ROI will not exceed with of Feeder
             * --------------------------------------------------------*/

            double x = Origin.X + (Width / 2) - PartOffset;
            double y = machine.Cal.ChannelRegion.Y;
            double z = machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE;
            /* Limit pick ROI width to width of feeder */
            double width = Width - PartOffset;
            double height = PICK_CHANNEL_HEIGHT;

            return new Position3D {  X = x, Y = y, Z = z, Width = width, Height = height };

        }

        private void updatePartPickLocation()
        {
            /*-----------------------------------------------------------
             * When the optical pick location is updated this routine will
             * update the Part Pick Location based on the current PicK Tool
             * and only fires when the optical location changes. TODO:
             * notify when Part Pick Tool isn't what's installed.  The offset
             * in Y that's applied is based on the fact that the tip is 
             * represented as a circle at is actually offset by it's radius
             * to the actual tip.
             * ---------------------------------------------------------*/

            Position3D offset_mm = machine.SelectedPickTool.GetToolTipOffsetAtZ(machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE);
            NextPartPickLocation.X = NextPartOpticalLocation.X - offset_mm.X;
            NextPartPickLocation.Y = NextPartOpticalLocation.Y + offset_mm.Y;
            NextPartPickLocation.Z = machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE - machine.SelectedPickTool.Length;
        }

        private void OnPartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on a part, force a get on the new part property
            OnPropertyChanged("Part");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
        
        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            //Console.WriteLine("Go To FeederModel Drive Position: " + x_drive + " mm " + y_drive + " mm");
            //machine.Messages.Add(GCommand.G_SetPosition(x_drive, y_drive, 0, 0, 0));
        }

        public ICommand PlacePartAtLocationCommand { get { return new RelayCommand(PlacePartAtLocation); } }
        public void PlacePartAtLocation()
        {

        }

        public ICommand RemoveFeederFromCassetteCommand { get { return new RelayCommand(RemoveFeederFromCassette); } }
        private void RemoveFeederFromCassette()
        {
            foreach (Part part in machine.PickList)
            {
                if (part.Equals(machine.SelectedCassette.SelectedFeeder.Part))
                    part.Cassette = null;
            }
            machine.SelectedCassette.Feeders.Remove(machine.SelectedCassette.SelectedFeeder);
        }

        public ICommand AdvanceNextComponentCommand { get { return new RelayCommand(AdvanceNextComponent); } }
        private void AdvanceNextComponent()
        {
            double y = (machine.Cal.DriveLineY + DriveOffset.Y);
            int low_feed = (int)(DRIVE_APPROACH_FEED_FACTOR * machine.Settings.RateXY);
            int advance_distance = (int)(DISTANCE_TO_TRAVEL_TIME * Interval);
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, y - 5, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetXYPosition(Origin.X, y, low_feed));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_DriveTapeAdvance(advance_distance));
            machine.Messages.Add(GCommand.Delay((int)(advance_distance * Constants.TAPE_DISTANCE_TO_TIME_MULTIPLIER)));
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, Origin.Y, 0, 0, 0));

        }

        public ICommand GoToNextComponentCommand { get { return new RelayCommand(GoToNextComponent); } }
        private void GoToNextComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(NextPartOpticalLocation.X, NextPartOpticalLocation.Y, 0, 0, 0));
        }

        public ICommand GoToNextPickComponentCommand { get { return new RelayCommand(GoToNextPickComponent); } }
        private void GoToNextPickComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_EnableValve(false));
            machine.Messages.Add(GCommand.G_SetPosition(NextPartPickLocation.X, NextPartPickLocation.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(NextPartPickLocation.Z, machine.Settings.ProbeRate));
        }

        public ICommand PickNextComponentCommand { get { return new RelayCommand(PickNextComponent); } }
        public void PickNextComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_EnablePump(true));
            machine.Messages.Add(GCommand.G_EnableValve(false));
            machine.Messages.Add(GCommand.G_SetPosition(NextPartPickLocation.X, NextPartPickLocation.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(NextPartPickLocation.Z, machine.Settings.ProbeRate));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(NextPartPickLocation.Z + PICK_TERMINAL_TRAVEL, 100));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_SetPosition(NextPartPickLocation.X, NextPartPickLocation.Y, 0, 0, 0));
        }

        public ICommand GoToFeederCommand { get { return new RelayCommand(GoToFeeder); } }
        public void GoToFeeder()
        {
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, machine.Cal.ChannelRegion.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
        }

        public ICommand SetCameraCaptureCommand { get { return new RelayCommand(SetCameraCapture); } }
        private void SetCameraCapture()
        {
            CaptureSettings = machine.downCamera.Settings.Clone();
        }
                
        public ICommand SetPartTemplateCommand { get { return new RelayCommand(SetPartTemplate); } }
        private void SetPartTemplate()
        {
            Console.WriteLine("Set Part Template");
            Mat mat = new Mat();
            Mat part_mat = new Mat();
            Cv2.CopyTo(machine.downCamera.CaptureImage, mat);
            using (OpenCvSharp.Window window = new OpenCvSharp.Window("Select ROI"))
            {
                window.Image = mat;

                // Use SelectROI to interactively select a region
                try
                {
                    OpenCvSharp.Rect roi = Cv2.SelectROI("Select ROI", mat);
                    // Capture the selected ROI
                    part_mat = new Mat(machine.downCamera.ColorImage, roi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                }

                // Write Part template to file
                DateTime now = DateTime.Now;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                String filename = FileUtils.ConvertToHashedFilename(QRCode, Constants.FEEDER_TEMPLATE_FILE_EXTENTION);
                Cv2.ImWrite(filename, part_mat);
                while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }
                Part.TemplateFileName = filename;

                string msg = string.Format("Part Template Saved Successfully.\n{0}", filename);
                ConfirmationDialog dlg = new ConfirmationDialog(msg);
                dlg.ShowDialog();
            }
        }
    }
}
