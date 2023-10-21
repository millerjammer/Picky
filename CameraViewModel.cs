using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Picky
{
    public class CameraViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        private readonly VideoCapture capture;

        public bool IsManualFocus { get; set; }

        public int Zoom { get; set; } = 1;

        private int focus;
        public int Focus
        {
            get { return focus; }
            set { focus = value; setCameraFocus(); PropertyChanged(this, new PropertyChangedEventArgs("Focus")); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CameraViewModel(VideoCapture cap)
        {
            this.capture = cap;
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
