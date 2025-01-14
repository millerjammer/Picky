using OpenCvSharp;
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
        public static double FEEDER_ORIGIN_TO_DRIVE_YOFFSET_MM = 50;
        public static double FEEDER_ORIGIN_TO_DRIVE_XOFFSET_MM = 2;
        
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
            /* Limit pick ROI width to width of feeder */
            double width = Width - PartOffset;

            return new Position3D {  X = x, Y = y, Width = width, Height = 40 };

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
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, Origin.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
        }

        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            Console.WriteLine("Go To FeederModel Drive Position: " + x_drive + " mm " + y_drive + " mm");
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
            machine.Messages.Add(GCommand.G_SetPosition(NextPartOpticalLocation.X, NextPartOpticalLocation.Y, 0, 0, 0));
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
            Mat part_mat = new Mat();
            Cv2.CopyTo(machine.downCamera.ColorImage, mat);
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
                String filename = path + "\\" + Part.Footprint + "-" + now.ToString("MMddHHmmss") + ".png";
                Cv2.ImWrite(filename, part_mat);
                while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }
                Part.TemplateFileName = filename;
            }
        }
    }
}
