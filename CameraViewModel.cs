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
    
    public class CameraViewModel : INotifyPropertyChanged
    {
        public MachineModel machine { get; set; }
        public CameraModel camera { get; set; }

        public List<VisualizationStyle> VisualizationView { get; set; }
        public List<ImageProcessingStyle> ImageProcessingView { get; set; }

        private VisualizationStyle selectedVisualizationViewItem;
        public VisualizationStyle SelectedVisualizationViewItem
        {
            get { return selectedVisualizationViewItem; }
            set { selectedVisualizationViewItem = value; camera.selectedViewMat = selectedVisualizationViewItem.viewMat; OnPropertyChanged(nameof(SelectedVisualizationViewItem)); } //Notify listeners
        }

        private ImageProcessingStyle selectedImageProcessingItem;
        public ImageProcessingStyle SelectedImageProcessingItem
        {
            get { return selectedImageProcessingItem; }
            set { selectedImageProcessingItem = value; OnPropertyChanged(nameof(SelectedImageProcessingItem)); } //Notify listeners
        }

        public SolidColorBrush PartInViewIconColor
        {
            get
            {
                if (machine?.SelectedCassette?.SelectedFeeder?.Part != null)
                {
                    if (machine.SelectedCassette.SelectedFeeder.Part.IsInView)
                    {
                        return (new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)));
                    }
                }
                return (new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)));
            }
        }
              
        public CameraViewModel(Image iFrame, CameraModel iCamera)
        {
            camera = iCamera;
            camera.FrameImage = iFrame;
            machine = MachineModel.Instance;

            /* Listen for SelectedCassette to change, then we can listen for SelectedFeeder */
            /* This should also fire when the camera image changes since camera is property of machine? */
            /* We use this to know what Part we're looking for */
            machine.PropertyChanged += OnMachinePropertyChanged;
            
            VisualizationView = new List<VisualizationStyle>
            {
                new VisualizationStyle("Normal", camera.ColorImage),
                new VisualizationStyle("Grayscale", camera.GrayImage),
                new VisualizationStyle("Threshold", camera.ThresImage),
                new VisualizationStyle("Edge Image", camera.EdgeImage),
                new VisualizationStyle("Dilated Image", camera.DilatedImage),
                new VisualizationStyle("ROI Match", camera.MatchImage),
                new VisualizationStyle("ROI Threshold", camera.MatchThresholdImage),
            };
            SelectedVisualizationViewItem = VisualizationView.FirstOrDefault();

            ImageProcessingView = new List<ImageProcessingStyle>
            {
                new ImageProcessingStyle("Normal"),
                new ImageProcessingStyle("ToolTipUpper"),
                new ImageProcessingStyle("QR Code"),
            };
            SelectedImageProcessingItem = ImageProcessingView.FirstOrDefault();
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
            if (machine?.SelectedCassette?.SelectedFeeder?.Part != null)
            {
                machine.downCamera.PartToFind = machine.SelectedCassette.SelectedFeeder.Part;
            }
        }
          
        public ICommand FullScreenCommand { get { return new RelayCommand(FullScreen); } }
        private void FullScreen()
        {
            Console.WriteLine("Full Screen Clicked");
        }

        public ICommand SetCircleDetectorCommand { get { return new RelayCommand(SetCircleDetector); } }
        private void SetCircleDetector()
        {
           machine.SaveSettings();
        }
    }
}
