using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;

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

        private void OnPartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on a _part, force a get on the new _part property
            OnPropertyChanged("part");
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            Console.WriteLine("part in feeder changed: " + e);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }

        public ICommand GoToFeederCommand { get { return new RelayCommand(GoToFeeder); } }
        private void GoToFeeder()
        {
            Console.WriteLine("Go To Feeder Position: " + x_origin + " mm " + y_origin + " mm  Part: " + part.Description);
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_origin, y_origin));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            Console.WriteLine("Go To Feeder Drive Position: " + x_drive + " mm " + y_drive + " mm");
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_drive, y_drive));
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
