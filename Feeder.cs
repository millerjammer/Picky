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
        /* Calibration Grid Size and Location of 1st */
        public static double FEEDER_8MM_WIDTH_MILS = 500;
        public static double FEEDER_ORIGIN_TO_DRIVE_YOFFSET_MM = 50;
        public static double FEEDER_ORIGIN_TO_DRIVE_XOFFSET_MM = 2;

        MachineModel machine = MachineModel.Instance;

        private Part _part;
        public Part part
        {
            get { return _part; }
            set { _part = value; Console.WriteLine("feeder part changed"); OnPropertyChanged(nameof(part)); }
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
            set
            {
                if (nextPartOpticalLocation != value)
                {
                    nextPartOpticalLocation = value;
                    OnPropertyChanged(nameof(NextPartOpticalLocation));
                }
            }
        }

        private Position3D nextPartPickLocation = new Position3D(0, 0, 0);
        public Position3D NextPartPickLocation
        {
            get { return nextPartPickLocation; }
            set
            {
                if (nextPartPickLocation != value)
                {
                    nextPartPickLocation = value;
                    OnPropertyChanged(nameof(NextPartPickLocation));
                }
            }
        }

        private double _x_origin;
        public double x_origin
        {
            get { return _x_origin; }
            set { _x_origin = value; OnPropertyChanged(nameof(x_origin)); }
        }
        private double _y_origin;
        public double y_origin
        {
            get { return _y_origin; }
            set { _y_origin = value; OnPropertyChanged(nameof(y_origin)); }
        }
        private double _z_origin;
        public double z_origin
        {
            get { return _z_origin; }
            set { _z_origin = value; OnPropertyChanged(nameof(z_origin)); }
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

        private string pickToolName;
        public string PickToolName
        {
            get { return pickToolName; }
            set { pickToolName = value; OnPropertyChanged(nameof(PickToolName)); }
        }


        public Feeder()
        {
            _part = new Part();
            _part.PropertyChanged += OnPartPropertyChanged;
        }

        public void SetCandidateNextPartPickLocation(double x_next, double y_next, double targetZ, double targetPickAngle)
        {
            /*----------------------------------------------------------------------
             * When part is in view, call here to update the part's position, optically
             * and pick tool. x and y are in terms of pixels in the current full frame.
             * This routine will convert the pixels to the center of the frame, add
             * the machine current position and calculate the offset too the selected 
             * tool head if available. targetZ is in mm and is location of part z
             * ---------------------------------------------------------------------*/

            // Get pixel offset relative to center of frame.
            double x_offset_pix = (x_next - (Constants.CAMERA_FRAME_WIDTH / 2));
            double y_offset_pix = (y_next - (Constants.CAMERA_FRAME_HEIGHT / 2));
            // Convert pixels to mm
            //var scale = machine.Cal.GetScaleMMPerPixAtZ(machine.Cal.TargetResAtPCB.MMHeightZ);
            var scale = machine.Cal.GetScaleMMPerPixAtZ(targetZ); //TODO fix this
            NextPartOpticalLocation.X = machine.CurrentX - (x_offset_pix * scale.xScale);
            NextPartOpticalLocation.Y = machine.CurrentY + (y_offset_pix * scale.yScale);

            //Now, if there's a tool, calculate offset to tool
            // Linearly interpolate X and Y offsets between the two circles based on Z
            PickToolModel tool = machine.SelectedPickTool;
            if (tool == null)
            {
                NextPartPickLocation.X = 0;
                NextPartPickLocation.Y = 0;
                return;
            }

            // Get offset relative to Tip ROI
            var offset = tool.GetTipOffsetForZ(targetZ, targetPickAngle);

            // Next, translate from ROI position to frame position in mm.
            double y_offset_roi = scale.yScale * ((Constants.CAMERA_FRAME_HEIGHT / 2) - tool.SearchToolROI.Y - (tool.SearchToolROI.Height / 2));
            double x_offset_roi = scale.xScale * ((Constants.CAMERA_FRAME_WIDTH / 2) - tool.SearchToolROI.X - (tool.SearchToolROI.Width / 2));

            // Add Tip offset and roi offset (all in mm)
            x_offset_roi += offset.x;
            y_offset_roi -= offset.y;

            // Finally, add the position of the part (we did above) relative to the center of the full frame.
            NextPartPickLocation.X = x_offset_roi + NextPartOpticalLocation.X;
            NextPartPickLocation.Y = y_offset_roi + NextPartOpticalLocation.Y;

            return;
        }

        private void OnPartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on a _part, force a get on the new _part property
            OnPropertyChanged("part");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }

        public ICommand GoToFeederCommand { get { return new RelayCommand(GoToFeeder); } }
        public void GoToFeeder()
        {
            Console.WriteLine("Go To Feeder Position: " + x_origin + " mm " + y_origin + " mm");
            machine.Messages.Add(GCommand.G_SetPosition(x_origin, y_origin, 0, 0, 0));
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
                if (part.Equals(machine.selectedCassette.selectedFeeder.part))
                    part.cassette = null;
            }
            machine.selectedCassette.Feeders.Remove(machine.selectedCassette.selectedFeeder);
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
                    part.Template = new Mat(machine.downCamera.ColorImage, roi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ouch - Memory Exception: " + ex.ToString());
                }

                // Write part template to file
                DateTime now = DateTime.Now;
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                String filename = path + "\\" + part.Footprint + "-" + now.ToString("MMddHHmmss") + ".png";
                Cv2.ImWrite(filename, part.Template);
                while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }
                part.TemplateFileName = filename;

            }
        }
    }
}
