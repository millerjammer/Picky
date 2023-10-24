using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Input;

namespace Picky
{
    public class CameraViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        private readonly VideoCapture capture;

        public bool IsManualFocus { get; set; }
       
        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; setCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

        private int zoom = 1;
        public int Zoom
        {
            get { return zoom; }
            set { zoom = value; OnPropertyChanged(nameof(Zoom)); }
        }

        public int DetectionThreshold
        {
            get
            {
                if (machine.selectedCassette != null && machine.selectedCassette.selectedFeeder != null)
                    return (int)machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold; return 0;
            }
            set { if (machine.selectedCassette == null || machine.selectedCassette.selectedFeeder == null) return; machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold = (double)value; OnPropertyChanged(nameof(DetectionThreshold)); }
        }
         
        public CameraViewModel(VideoCapture cap)
        {
            this.capture = cap;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            machine.selectedCassette.PropertyChanged += OnCassettePropertyChanged;
        }

        private void OnCassettePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           // Something changed on a Cassette, force a get on the newly selected Feeder property
            OnPropertyChanged("DetectionThreshold");
        }

        public void setCameraFocus()
        {
            capture.AutoFocus = !IsManualFocus;
            if (IsManualFocus)
            {
                int value = (int)capture.Get(VideoCaptureProperties.Focus);
                capture.Set(VideoCaptureProperties.Focus, Focus);
                Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                Console.WriteLine("Auto Focus Mode");
            }
        }

        public ICommand FullScreenCommand { get { return new RelayCommand(FullScreen); } }
        private void FullScreen()
        {
            Console.WriteLine("Full Screen Clicked");
        }

        public ICommand AutoFocusCommand { get { return new RelayCommand(AutoFocus); } }
        private void AutoFocus()
        {
            setCameraFocus(); 
        }
    }
}
