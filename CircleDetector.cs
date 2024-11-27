using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class CircleDetector : INotifyPropertyChanged
    {
        private int param1 = Constants.TOOL_DETECTOR_P1;
        public int Param1
        {
            get { return param1; }
            set { param1 = value; OnPropertyChanged(nameof (Param1)); }
        }

        private double param2 = Constants.TOOL_DETECTOR_P2;
        public double Param2
        {
            get { return param2; }
            set { param2 = value; OnPropertyChanged(nameof(Param2)); }
        }

        private int threshold = 200;
        public int Threshold
        {
            get { return threshold; }
            set { threshold = value; OnPropertyChanged(nameof(Threshold)); }
        }

        private int focus = 200;
        public int Focus
        {
            get { return focus; }
            set { focus = value; OnPropertyChanged(nameof(Focus)); }
        }

        private bool isManualFocus = true;
        public bool IsManualFocus
        {
            get { return isManualFocus; }
            set { isManualFocus = value; OnPropertyChanged(nameof(IsManualFocus)); }
        }

        public HoughModes DetectorType;
        public double zEstimate;                    //In mm to optical plane
        public double Radius;                       //In mm
        public Rect ROI;                            //In mm
        public int Count;                           //Number of circles to find

        public CircleDetector(HoughModes mode, int param1, double param2, int threshold)
        {
            DetectorType = mode;
            Param1 = param1;
            Param2 = param2;
            Threshold = threshold;
            IsManualFocus = true;
        }

        public CircleDetector(HoughModes mode, int param1, double param2, int threshold, int focus)
        {
            DetectorType = mode;
            Param1 = param1;
            Param2 = param2;
            Threshold = threshold;
            IsManualFocus = false;
            Focus = focus;
        }

        public CircleDetector()
        {
            DetectorType = HoughModes.Gradient;
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
