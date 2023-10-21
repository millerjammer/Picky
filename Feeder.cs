using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public double x_origin { get; set; }
        public double y_origin { get; set; }
        public double z_origin { get; set; }

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
            // Handle the property change notification
            Console.WriteLine("Part property changed: ");

            // Notify the Grandparent by invoking a method or event
            OnPropertyChanged("part");
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            //Console.WriteLine("part in feeder changed");
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

                // Display the selected ROI
                // Cv2.ImShow("Selected ROI", part.Template);

                // Write part template to file
                DateTime now = DateTime.Now;
                part.TemplateFileName = part.Footprint + "-" + now.ToString("MMddHHmmss") + ".png";
                Cv2.ImWrite(part.TemplateFileName, part.Template);
            }
        }
    }
}
