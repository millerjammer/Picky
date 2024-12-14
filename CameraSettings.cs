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
        private bool isManualFocus;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; SetCameraFocus(); OnPropertyChanged(nameof(IsManualFocus)); }
        }

        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; SetCameraFocus(); OnPropertyChanged(nameof(Focus)); }
        }

        private int binaryThreshold;
        public int BinaryThreshold
        {
            get { return binaryThreshold; }
            set { binaryThreshold = value; OnPropertyChanged(nameof(BinaryThreshold)); }
        }

        private int templateThreshold;
        public int TemplateThreshold
        {
            get { return templateThreshold; }
            set { templateThreshold = value; OnPropertyChanged(nameof(TemplateThreshold)); }
        }

        private int circleDetectorP1;
        public int CircleDetectorP1
        {
            get { return circleDetectorP1; }
            set { circleDetectorP1 = value; OnPropertyChanged(nameof(CircleDetectorP1)); }
        }

        private double circleDetectorP2;
        public double CircleDetectorP2
        {
            get { return circleDetectorP2; }
            set { circleDetectorP2 = value; OnPropertyChanged(nameof(CircleDetectorP2)); }
        }

        public VideoCapture Capture { get; set; }

        public CameraSettings(int cameraIndex) 
        {
            Capture = new VideoCapture();

            Capture.Open(cameraIndex);
            Capture.Set(VideoCaptureProperties.Fps, Constants.CAMERA_FPS);
            Capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('m', 'j', 'p', 'g'));
            Capture.Set(VideoCaptureProperties.FourCC, OpenCvSharp.VideoWriter.FourCC('M', 'J', 'P', 'G'));

            Capture.Set(VideoCaptureProperties.FrameWidth, Constants.CAMERA_FRAME_WIDTH);
            Capture.Set(VideoCaptureProperties.FrameHeight, Constants.CAMERA_FRAME_HEIGHT);
            Capture.Set(VideoCaptureProperties.AutoExposure, 1);
            Capture.Set(VideoCaptureProperties.Brightness, .2);
            Capture.Set(VideoCaptureProperties.BufferSize, 200);
        }

        private void SetCameraFocus()
        {
            if (IsManualFocus == true)
            {
                int value = (int)Capture.Get(VideoCaptureProperties.Focus);
                Capture.Set(VideoCaptureProperties.Focus, Focus);
                Console.WriteLine("Manual Focus " + value + " -> " + Focus);
            }
            else
            {
                Capture.Set(VideoCaptureProperties.AutoFocus, 1);
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
