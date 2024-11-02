using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class RectangleDetector
    {
        public double Threshold1;       // For Edge Detection (start w/50);
        public double Threshold2;       // For Edge Detection (start w/100);
        
        public OpenCvSharp.Rect ROI;                //In mm

        public RectangleDetector(double param1, double param2)
        {
            Threshold1 = param1;
            Threshold2 = param2;
        }
    }
}
