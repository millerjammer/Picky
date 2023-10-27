using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Linq;
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

        private bool isCassetteFeederSelected = false;
        public bool IsCassetteFeederSelected 
        {  
            get { return isCassetteFeederSelected;  }
        }

        private int detectionThreshold;
        public int DetectionThreshold
        {
            get { return detectionThreshold;  }
            set { detectionThreshold = value; machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold = (double)detectionThreshold;  OnPropertyChanged(nameof(DetectionThreshold)); }
        }

        public CameraViewModel(VideoCapture cap)
        {
            this.capture = cap;
            /* Listen for selectedCassette to change, then we can listen for selectedFeeder */ 
            machine.PropertyChanged += OnMachinePropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
               
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on the machine, notify the view to update itself
            if (machine.selectedCassette == null || machine.selectedCassette.selectedFeeder == null)
            {
                isCassetteFeederSelected = false;
            }
            else
            {
                /* Machine changed, listen to cassett property changes */
                machine.selectedCassette.PropertyChanged += OnMachinePropertyChanged;
                /* TODO, get rid of this, do in setter/getter */
                detectionThreshold = (int)machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold;
                isCassetteFeederSelected = true;
            }
            OnPropertyChanged("IsCassetteFeederSelected");
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
