using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace Picky
{
    public class VisualizationStyle
    {
        public string viewName { get; set; }
        public Mat viewMat { get; set; }
        public VisualizationStyle(string name, Mat mati)
        {
            viewName = name;
            viewMat = mati;
        }
    }

    public class CameraViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        VideoCapture capture;

        public Mat cameraImage = new Mat();
        public Mat grayImage = new Mat();
        public Mat thresImage = new Mat();
        public Mat edgeImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
        public Mat dilatedImage = new Mat(0, 0, Constants.CAMERA_FRAME_WIDTH, Constants.CAMERA_FRAME_HEIGHT);
        public Mat pickROI = new Mat();

        public SolidColorBrush PartInViewIconColor
        {
            get { if(IsPartInView) return (new SolidColorBrush(Color.FromArgb(128, 0, 255, 0))); else return (new SolidColorBrush(Color.FromArgb(128, 255, 0, 0))); }
        }

        private bool isPartInView = false;
        public bool IsPartInView 
        {
            get { return isPartInView; }
            set { if(isPartInView == value) return; isPartInView = value; OnPropertyChanged(nameof(PartInViewIconColor)); }
        }

        private bool isManualFocus;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; setCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }

        public List<VisualizationStyle> VisualizationView { get; set; }
        public VisualizationStyle SelectedVisualizationViewItem {  get; set; }

        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; setCameraFocus(); OnPropertyChanged(nameof(Focus)); }
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

            VisualizationView = new List<VisualizationStyle>
            {
                new VisualizationStyle("Normal View", cameraImage),
                new VisualizationStyle("Grayscale", grayImage),
                new VisualizationStyle("Threshold", thresImage),
                new VisualizationStyle("Edge Image", edgeImage),
                new VisualizationStyle("Dilated Image", dilatedImage),
            };
            SelectedVisualizationViewItem = VisualizationView.FirstOrDefault();
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
    
    }
}
