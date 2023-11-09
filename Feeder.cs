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

namespace Picky
{
    public class Feeder : INotifyPropertyChanged
    {
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

        private double _x_next_part;
        public double x_next_part
        {
            get { return _x_next_part; }
            set { 
                if( _x_next_part != value)
                {
                    _x_next_part = value;
                    OnPropertyChanged(nameof(x_next_part)); 
                }
            }
        }
        private double _y_next_part;
        public double y_next_part
        {
            get { return _y_next_part; }
            set {
                if (_y_next_part != value)
                {
                    _y_next_part = value;
                    OnPropertyChanged(nameof(y_next_part));
                }
            }
        }
        private double _z_next_part;
        public double z_next_part
        {
            get { return _z_next_part; }
            set
            {
                if (_z_next_part != value)
                {
                    _z_next_part = value;
                    OnPropertyChanged(nameof(z_next_part));
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
        
        public double x_drive { get; set; }
        public double y_drive { get; set; }
        public double z_drive { get; set; }

        
        public Feeder()
        {
            _part = new Part();
            _part.PropertyChanged += OnPartPropertyChanged;
        }

        public bool SetCandidateNextPartLocation(double x, double y)
        {
            /* See if this falls in the correct range */
            double left = x_origin - (width / 2);
            double right = x_origin + (width / 2);
            if( x > left && x < right) 
            {
                if( y < (y_origin + Constants.FEEDER_ORIGIN_TO_PART_TRAY_START) && y > (y_origin + Constants.FEEDER_ORIGIN_TO_PART_TRAY_END)){
                    x_next_part = x; y_next_part = y;
                    return true;
                }
            }
            x_next_part = 0; y_next_part = 0;
            return false; ;
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
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_origin, y_origin));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            Console.WriteLine("Go To Feeder Drive Position: " + x_drive + " mm " + y_drive + " mm");
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_drive, y_drive));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(z_drive));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand PlacePartAtLocationCommand { get { return new RelayCommand(PlacePartAtLocation); } }
        public void PlacePartAtLocation()
        {
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());

            double partx = machine.PCB_OriginX + (Convert.ToDouble(machine.selectedPickListPart.CenterX) * Constants.MIL_TO_MM);
            double party = machine.PCB_OriginY + (Convert.ToDouble(machine.selectedPickListPart.CenterY) * Constants.MIL_TO_MM);
            double angle = Convert.ToDouble(machine.selectedPickListPart.Rotation);
            double z = 14;
            Tuple<double, double> offset = machine.SelectedPickTool.GetPickOffsetAtRotation(angle);
            /* TODO is this the right z to use?  Or, perhaps use 'z' above? */
            
            partx += Constants.PLACE_DISTORTION_OFFSET_X_MM - (offset.Item1 * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z));
            party += Constants.PLACE_DISTORTION_OFFSET_Y_MM - (offset.Item2 * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z));
            
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(partx, party));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteAngle(angle / Constants.B_DEGREES_PER_MM));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(z));
            machine.Messages.Add(Command.S3G_GetPosition());
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

        public ICommand GoToNextComponentCommand { get { return new RelayCommand(GoToNextComponent); } }
        private void GoToNextComponent()
        {
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
            if (x_next_part != 0 && y_next_part != 0)
            {
                machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_next_part, y_next_part));
                machine.Messages.Add(Command.S3G_GetPosition());
            }
        }

        public ICommand PickNextComponentCommand { get { return new RelayCommand(PickNextComponent); } }
        public void PickNextComponent()
        {
            double pickX, pickY;
            Tuple<double, double> offset = machine.SelectedPickTool.GetPickOffsetAtRotation(0);
            
            if (x_next_part != 0 && y_next_part != 0)
            {
                pickX = x_next_part + Constants.PICK_DISTORTION_OFFSET_X_MM - (offset.Item1 * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z));
                pickY = y_next_part + Constants.PICK_DISTORTION_OFFSET_Y_MM - (offset.Item2 * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z));
                Console.WriteLine("HEY offset is: " + (offset.Item1 * machine.GetImageScaleAtDistanceX(Constants.PART_TO_PICKUP_Z)) + " mm " + (offset.Item2 * machine.GetImageScaleAtDistanceY(Constants.PART_TO_PICKUP_Z)));
                Console.WriteLine("Pick next: " + pickX + " mm " + pickY + " mm");
                machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(pickX, pickY));
                machine.Messages.Add(Command.S3G_GetPosition());
                machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0));
                machine.Messages.Add(Command.S3G_GetPosition());
                machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.PART_TO_PICKUP_Z));
                machine.Messages.Add(Command.S3G_GetPosition());
            }
        }

        public void PickNextComponentOptically()
        {
            /* This function loads commands to cause the serial command dispatcher to use values from the camera as the component's x, y */
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPositionOptically(this));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteAngle(0));
            machine.Messages.Add(Command.S3G_GetPosition());
            machine.Messages.Add(Command.S3G_SetAbsoluteZPosition(Constants.PART_TO_PICKUP_Z));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand SetPartTemplateCommand { get { return new RelayCommand(SetPartTemplate); } }
        private void SetPartTemplate()
        {
            Console.WriteLine("Set Part Template");
            using (Window window = new Window("Select ROI"))
            {
                window.Image = machine.currentRawImage;

                // Use SelectROI to interactively select a region
                OpenCvSharp.Rect roi = Cv2.SelectROI("Select ROI", machine.currentRawImage);

                // Capture the selected ROI
                part.Template = new Mat(machine.currentRawImage, roi);

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
