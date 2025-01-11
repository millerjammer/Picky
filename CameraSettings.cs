using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Picky
{
    public class CameraSettings : INotifyPropertyChanged
    {
        private bool isManualFocus = false;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; SetCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }

        private int focus = 393;
        public int Focus
        {
            get { return focus; }
            set { focus = value; SetCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

        private int binaryThreshold = 100;
        public int BinaryThreshold
        {
            get { return binaryThreshold; }
            set { binaryThreshold = value; OnPropertyChanged(nameof(BinaryThreshold)); }
        }

        private int templateThreshold = -190;
        public int TemplateThreshold
        {
            get { return templateThreshold; }
            set { templateThreshold = value; OnPropertyChanged(nameof(TemplateThreshold)); }
        }

        private int circleDetectorP1 = 100;
        public int CircleDetectorP1
        {
            get { return circleDetectorP1; }
            set { circleDetectorP1 = value; OnPropertyChanged(nameof(CircleDetectorP1)); }
        }

        private double circleDetectorP2 = 0.65;
        public double CircleDetectorP2
        {
            get { return circleDetectorP2; }
            set { circleDetectorP2 = value; OnPropertyChanged(nameof(CircleDetectorP2)); }
        }

        private CameraModel camera;
           

        public CameraSettings() 
        {
            
        }

        public void ApplySettings(CameraModel _camera)
        {
            camera = _camera;
            SetCameraFocus();
        }


        private void SetCameraFocus()
        {
            if(camera == null)
            {
                return;
            }
            if (IsManualFocus == true)
            {
                int value = (int)camera.Capture.Get(VideoCaptureProperties.Focus);
                camera.Capture.Set(VideoCaptureProperties.Focus, Focus);
                Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                camera?.Capture?.Set(VideoCaptureProperties.AutoFocus, 1);
            }
        }

        public CameraSettings Clone() => (CameraSettings)this.MemberwiseClone();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}
