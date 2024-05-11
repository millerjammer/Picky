using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

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

        CameraModel camera;
                    
        public List<VisualizationStyle> VisualizationView { get; set; }
        //public VisualizationStyle SelectedVisualizationViewItem {  get; set; }

        private VisualizationStyle selectedVisualizationViewItem;
        public VisualizationStyle SelectedVisualizationViewItem
        {
            get { return selectedVisualizationViewItem; }
            set { selectedVisualizationViewItem = value; camera.selectedViewMat = selectedVisualizationViewItem.viewMat; OnPropertyChanged(nameof(SelectedVisualizationViewItem)); } //Notify listeners
        }


        //private bool isPartInView = false;
        //public bool IsPartInView
        //{
        //  get { return machine.selectedCassette.selectedFeeder.part.IsInView }
        //  set { if (isPartInView == value) return; isPartInView = value; OnPropertyChanged(nameof(PartInViewIconColor)); }
        // }


        public SolidColorBrush PartInViewIconColor
        {
            get
            {
                if (machine?.selectedCassette?.selectedFeeder?.part != null)
                {
                    if (machine.selectedCassette.selectedFeeder.part.IsInView)
                    {
                        return (new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)));
                    }
                }
                return (new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)));
            }
        }

        public bool IsManualFocus
        {
            get { return camera.IsManualFocus; }
            set { camera.IsManualFocus = value; setCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }

        public int Focus
        {
            get { return camera.Focus; }
            set { camera.Focus = value; setCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

        private int detectionThreshold;
        public int DetectionThreshold
        {
            get { return detectionThreshold; }
            set { detectionThreshold = value; machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold = (double)detectionThreshold; OnPropertyChanged(nameof(DetectionThreshold)); }
        }

        

       // private bool isCassetteFeederSelected = false;
       // public bool IsCassetteFeederSelected 
       // {  
       //     get { return isCassetteFeederSelected;  }
       // }
        

        public CameraViewModel(Image iFrame, CameraModel iCamera)
        {
           
            camera = iCamera;
            camera.FrameImage = iFrame;
                      

            /* Listen for selectedCassette to change, then we can listen for selectedFeeder */
            /* This should also fire when the camera image changes since camera is property of machine? */
            /* We use this to know what part we're looking for */
            machine.PropertyChanged += OnMachinePropertyChanged;

            VisualizationView = new List<VisualizationStyle>
            {
                new VisualizationStyle("Normal", camera.ColorImage),
                new VisualizationStyle("Grayscale", camera.GrayImage),
                new VisualizationStyle("Threshold", camera.ThresImage),
                new VisualizationStyle("Edge Image", camera.EdgeImage),
                new VisualizationStyle("Dilated Image", camera.DilatedImage),
            };
            SelectedVisualizationViewItem = VisualizationView.FirstOrDefault();

            //CameraIndexView = new List<CameraSelection>
            //{
             //   new CameraSelection("Up Camera", captureUp),
             //   new CameraSelection("Down Camera", captureDown),
            //};
            //SelectedCameraIndexViewItem = CameraIndexView.FirstOrDefault();

            
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
               
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Something changed on the machine, notify the view to update itself
            // if (machine.selectedCassette == null || machine.selectedCassette.selectedFeeder == null)
            // {
            //     isCassetteFeederSelected = false;
            // }
            // else
            // {
            /* Machine changed, listen to cassett property changes */
            //     machine.selectedCassette.PropertyChanged += OnMachinePropertyChanged;
            /* TODO, get rid of this, do in setter/getter */
            //     camera.detectionThreshold = (int)machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold;
            //     isCassetteFeederSelected = true;
            // }
            // OnPropertyChanged("IsCassetteFeederSelected");
            //OnPropertyChanged("DetectionThreshold");

            //machine property changed so update inview icon color since this is what the view is bound to.
            OnPropertyChanged(nameof(PartInViewIconColor));
            if (machine?.selectedCassette?.selectedFeeder?.part != null)
            {
                machine.downCamera.PartToFind = machine.selectedCassette.selectedFeeder.part;
            }
            //Console.WriteLine("machine change - " + sender.ToString());
        }
        
        public void setCameraFocus()
        {
            camera.IsManualFocus = IsManualFocus;
            if (IsManualFocus)
            {
               int value = (int)camera.capture.Get(VideoCaptureProperties.Focus);
               camera.capture.Set(VideoCaptureProperties.Focus, Focus);
               Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                camera.capture.Set(VideoCaptureProperties.AutoFocus, 1);
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
