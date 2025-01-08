using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using System.Windows;

namespace Picky
{
    public class Feeder : INotifyPropertyChanged
    {
        /* Calibration GridOrigin Size and Location of 1st */
        public static double FEEDER_8MM_WIDTH_MILS = 500;
        public static double FEEDER_ORIGIN_TO_DRIVE_YOFFSET_MM = 50;
        public static double FEEDER_ORIGIN_TO_DRIVE_XOFFSET_MM = 2;
        public static double PICK_CHANNEL_OFFSET_Y_MM = 10;
        public static double PICK_CHANNEL_WIDTH_MM = 10;
        public static double PICK_CHANNEL_LENGTH_MM = 40;

        MachineModel machine = MachineModel.Instance;

        private Part part;
        public Part Part
        {
            get { return part; }
            set { part = value; OnPropertyChanged(nameof(Part)); }
        }

        public int index { get; set; }
        public int start_count { get; set; }
        public int placed_count { get; set; }
        public double width { get; set; }
        public double thickness { get; set; }
        public double interval { get; set; }

        private Position3D nextPartOpticalLocation = new Position3D(0, 0, 0);
        public Position3D NextPartOpticalLocation
        {
            get { return nextPartOpticalLocation; }
            set { nextPartOpticalLocation = value; OnPropertyChanged(nameof(NextPartOpticalLocation));  }
        }

        private Position3D nextPartPickLocation = new Position3D(0, 0, 0);
        public Position3D NextPartPickLocation
        {
            get { return nextPartPickLocation; }
            set { nextPartPickLocation = value; OnPropertyChanged(nameof(NextPartPickLocation)); }
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

        public double x_drive { get; set; }
        public double y_drive { get; set; }

        private PickToolModel pickTool;
        public PickToolModel PickTool
        {
            get { return pickTool; }
            set { pickTool = value; OnPropertyChanged(nameof(PickTool)); }
        }


        public Feeder()
        {
            part = new Part();
            part.PropertyChanged += OnPartPropertyChanged;
        }

        public Position3D GetPickROI()
        {
            double x = Origin.X + (PICK_CHANNEL_WIDTH_MM / 2);
            double y = Origin.Y + PICK_CHANNEL_OFFSET_Y_MM;
            return new Position3D {  X = x, Y = y, Width = PICK_CHANNEL_WIDTH_MM, Height = PICK_CHANNEL_LENGTH_MM };

        }

        public void SetCandidateNextPartPickLocation(double x_next, double y_next, double targetZ)
        {
            /*----------------------------------------------------------------------
             * When Part is in view, call here to update the Part's position, optically
             * and pick tool. x and y are in terms of pixels in the current full frame.
             * This routine will convert the pixels to the center of the frame, add
             * the machine current position and calculate the offset too the selected 
             * tool head if available. targetZ is in mm and is location of Part z
             * ---------------------------------------------------------------------*/

            // Get pixel offset relative to center of frame.
            double x_offset_pix = (x_next - (Constants.CAMERA_FRAME_WIDTH / 2));
            double y_offset_pix = (y_next - (Constants.CAMERA_FRAME_HEIGHT / 2));

            // Convert pixels to mm
            var scale = machine.Cal.GetScaleMMPerPixAtZ(targetZ);
            NextPartOpticalLocation.X = machine.CurrentX - (x_offset_pix * scale.xScale);
            NextPartOpticalLocation.Y = machine.CurrentY + (y_offset_pix * scale.yScale);

            // Now, if there's a tool, calculate offset to tool
            if (machine.SelectedPickTool == null)
            {
                NextPartPickLocation.X = 0;
                NextPartPickLocation.Y = 0;
                return;
            }
            PickToolModel tool = machine.SelectedPickTool;
            NextPartPickLocation.Z = Constants.ZOFFSET_CAL_PAD_TO_FEEDER_TAPE + tool.UpperCal.TipPosition.Z;
            Position3D pos = machine.SelectedPickTool.GetToolTipOffsetAtZ(NextPartPickLocation.Z);
            NextPartPickLocation.X = NextPartOpticalLocation.X - pos.X;
            NextPartPickLocation.Y = NextPartOpticalLocation.Y + pos.Y;
            return;
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

        public ICommand GoToFeederCommand { get { return new RelayCommand(GoToFeeder); } }
        public void GoToFeeder()
        {
            Console.WriteLine("Go To Feeder Position: " + Origin.X + " mm " + Origin.Y + " mm");
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, Origin.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.GetFeederQRCode(this));
        }

        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            Console.WriteLine("Go To Feeder Drive Position: " + x_drive + " mm " + y_drive + " mm");
            machine.Messages.Add(GCommand.G_SetPosition(x_drive, y_drive, 0, 0, 0));
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

        public ICommand GoToNextPickComponentCommand { get { return new RelayCommand(GoToNextPickComponent); } }
        private void GoToNextPickComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(NextPartPickLocation.X, NextPartPickLocation.Y, 0, 0, 0));
        }

        public ICommand GoToNextComponentCommand { get { return new RelayCommand(GoToNextComponent); } }
        private void GoToNextComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.OpticallyAlignToPart(this));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
        }

        public ICommand PickNextComponentCommand { get { return new RelayCommand(PickNextComponent); } }
        public void PickNextComponent()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.AddFeederPickToQueue(this);
        }

        public ICommand SetPartTemplateCommand { get { return new RelayCommand(SetPartTemplate); } }
        private void SetPartTemplate()
        {
            Console.WriteLine("Set Part Template");
            Mat mat = new Mat();
            Cv2.CopyTo(machine.downCamera.ColorImage, mat);
            using (OpenCvSharp.Window window = new OpenCvSharp.Window("Select ROI"))
            {
                window.Image = mat;

                // Use SelectROI to interactively select a region
                try
                {
                    OpenCvSharp.Rect roi = Cv2.SelectROI("Select ROI", mat);
                    // Capture the selected ROI
                    Part.Template = new Mat(machine.downCamera.ColorImage, roi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                }

                // Write Part template to file
                DateTime now = DateTime.Now;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                String filename = path + "\\" + Part.Footprint + "-" + now.ToString("MMddHHmmss") + ".png";
                Cv2.ImWrite(filename, Part.Template);
                while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }
                Part.TemplateFileName = filename;

            }
        }
    }
}
