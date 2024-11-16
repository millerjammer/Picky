using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Security.RightsManagement;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

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

        public void SetCandidateNextPartLocation(double x_next, double y_next, double targetZ)
        {
            /*----------------------------------------------------------------------
             * When part is in view, call here to update the part's position, optically
             * and pick tool. x and y are in terms of pixels in the current full frame.
             * This routine will convert the pixels to the center of the frame, add
             * the machine current position and calculate the offset too the selected 
             * tool head if available. targetZ is in mm and is location of part z
             * ---------------------------------------------------------------------*/
            Application.Current.Dispatcher.Invoke(() =>
            {
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
            if (machine.SelectedPickTool == null)
            {
                NextPartPickLocation.X = 0;
                NextPartPickLocation.Y = 0;
                return;
            }
            PickToolModel tool = machine.SelectedPickTool;

            // Use slope (rise over run) to get x, y at targetZ.  x, y is measured in mm from center of ROI
            // Calculate offset to the pick from the center of the full frame
            // Start by calculating the pick location (mm) of the tip at expected z in the ROI
            double slopeX = (tool.TipOffsetLower.BestCircle.X - tool.TipOffsetUpper.BestCircle.X) / (tool.TipOffsetLower.BestCircle.Z - tool.TipOffsetUpper.BestCircle.Z);
            double x = (tool.TipOffsetUpper.BestCircle.X + (slopeX * (targetZ - tool.TipOffsetUpper.BestCircle.Z)));
            double slopeY = (tool.TipOffsetLower.BestCircle.Y - tool.TipOffsetUpper.BestCircle.Y) / (tool.TipOffsetLower.BestCircle.Z - tool.TipOffsetUpper.BestCircle.Z);
            double y = (tool.TipOffsetUpper.BestCircle.Y + (slopeY * (targetZ - tool.TipOffsetUpper.BestCircle.Z)));
            double slopeR = (tool.TipOffsetLower.BestCircle.Radius - tool.TipOffsetUpper.BestCircle.Radius) / (tool.TipOffsetLower.BestCircle.Z - tool.TipOffsetUpper.BestCircle.Z);
            double radius = (tool.TipOffsetUpper.BestCircle.Radius + (slopeR * (targetZ - tool.TipOffsetUpper.BestCircle.Radius)));

                


                // Next, translate from ROI position to frame position in mm.
                y += scale.yScale * ((Constants.CAMERA_FRAME_HEIGHT / 2) - tool.SearchToolROI.Y - (tool.SearchToolROI.Height / 2));
                x += scale.xScale * ((Constants.CAMERA_FRAME_WIDTH / 2) - tool.SearchToolROI.X - (tool.SearchToolROI.Width / 2));

                // Convert angle to radians, add radius - not sure if signs are correct
                double angle = machine.CurrentA;
                double angleInRadians = angle * (Math.PI / 180.0);
                x -= tool.TipOffsetUpper.BestCircle.Radius * Math.Cos(angleInRadians);
                y += tool.TipOffsetUpper.BestCircle.Radius * Math.Sin(angleInRadians);

                //Console.WriteLine("rx, ry:  " + tool.TipOffsetUpper.BestCircle.Radius * Math.Cos(angleInRadians) + " " + tool.TipOffsetUpper.BestCircle.Radius * Math.Sin(angleInRadians));

                // Scale and offset for unaccounted items
                y *= 1.0;  //At focus 381, calibrated circle at 475
                y -= 1.3;
                x *= 1.0;
                x += 0.1;

                // Next, add the position of the part (we did above) relative to the center of the full frame.
                NextPartPickLocation.X = x + NextPartOpticalLocation.X;
                NextPartPickLocation.Y = y + NextPartOpticalLocation.Y;
                
//Console.WriteLine("x, y:  " + x + " " + y + " " + tool.SearchToolROI.ToString());
                //double t = (targetZ - tool.TipOffsetUpper.Z) / (tool.TipOffsetLower.Z - tool.TipOffsetUpper.Z);

                // Interpolated x and y offsets
                //double interpolatedX = (tool.TipOffsetUpper.X) + t * ((tool.TipOffsetLower.X - tool.TipOffsetUpper.X));
                // double interpolatedY = (tool.TipOffsetUpper.Y) + t * ((tool.TipOffsetLower.Y - tool.TipOffsetUpper.Y));

                // Convert rotation from degrees to radians
                // double rotationRadians = double.Parse(part.Rotation) * (Math.PI / 180);

                // Apply rotation
                // NextPartPickLocation.X = x * Math.Cos(rotationRadians) - y * Math.Sin(rotationRadians);
                //NextPartPickLocation.Y = x * Math.Sin(rotationRadians) + y * Math.Cos(rotationRadians);

                //TODO - ROI should be set as a default of the tool model

                //NextPartPickLocation.X = x + x_offset_pix;
                //NextPartPickLocation.Y = y + y_offset_pix;


//Console.WriteLine("offset from center of full frame [px]:  " + NextPartPickLocation.X + " " + NextPartPickLocation.Y);

                // Location is relative to the center of the ROI.  Calculate offset relative to center of full frame
                //NextPartPickLocation.Y += (Constants.CAMERA_FRAME_HEIGHT / 2) - tool.UpperCircleDetector.ROI.Y - (tool.UpperCircleDetector.ROI.Height/2);
                //NextPartPickLocation.X ; 

                // Apply scale
                //NextPartPickLocation.X *= scale.xScale;
                //NextPartPickLocation.Y *= scale.yScale;
//Console.WriteLine("head offset, from center of Frame [mm]: " + NextPartPickLocation.X + " " + NextPartPickLocation.Y);

                // Add current position
                //NextPartPickLocation.X += machine.CurrentX;
                //NextPartPickLocation.Y += machine.CurrentY;
            });
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
                catch(Exception ex)
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
