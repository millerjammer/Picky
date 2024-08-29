using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
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

        public CameraModel camera { get; set; }

        public List<VisualizationStyle> VisualizationView { get; set; }

        private VisualizationStyle selectedVisualizationViewItem;
        public VisualizationStyle SelectedVisualizationViewItem
        {
            get { return selectedVisualizationViewItem; }
            set { selectedVisualizationViewItem = value; camera.selectedViewMat = selectedVisualizationViewItem.viewMat; OnPropertyChanged(nameof(SelectedVisualizationViewItem)); } //Notify listeners
        }

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

        

        private int detectionThreshold;
        public int DetectionThreshold
        {
            get { return detectionThreshold; }
            set { detectionThreshold = value; machine.selectedCassette.selectedFeeder.part.PartDetectionThreshold = (double)detectionThreshold; OnPropertyChanged(nameof(DetectionThreshold)); }
        }

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
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(PartInViewIconColor));
            if (machine?.selectedCassette?.selectedFeeder?.part != null)
            {
                machine.downCamera.PartToFind = machine.selectedCassette.selectedFeeder.part;
            }
        }
              

        public ICommand FullScreenCommand { get { return new RelayCommand(FullScreen); } }
        private void FullScreen()
        {
            Console.WriteLine("Full Screen Clicked");
        }


    }
}
