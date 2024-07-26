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

        public void SetCandidateNextPartLocation(double x, double y)
        {
            y_next_part = y;
            x_next_part = x;
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

        public ICommand GoToNextComponentCommand { get { return new RelayCommand(GoToNextComponent); } }
        private void GoToNextComponent()
        {
            machine.Messages.Add(GCommand.G_SetPosition(x_next_part, y_next_part, 0, 0, 0));
        }

        public ICommand PickNextComponentCommand { get { return new RelayCommand(PickNextComponent); } }
        public void PickNextComponent()
        {
            var offset = machine.Cal.GetPickHeadOffsetToCamera(125.0);
            Console.WriteLine("Pick Head / Camera Offset: " + offset.ToString());
            double x = x_next_part + offset.x_offset;
            double y = y_next_part + offset.y_offset;
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(125.0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_EnablePump(true));
            machine.Messages.Add(GCommand.G_EnableValve(false));
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
        }

        public void PickNextComponentOptically()
        {
            /* This function loads commands to cause the serial command dispatcher to use values from the camera as the component's x, y */
            
        }

        public ICommand SetPartTemplateCommand { get { return new RelayCommand(SetPartTemplate); } }
        private void SetPartTemplate()
        {
            Console.WriteLine("Set Part Template");
            Mat mat = new Mat();
            Cv2.CopyTo(machine.downCamera.ColorImage, mat);
            using (Window window = new Window("Select ROI"))
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
