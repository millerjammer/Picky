using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class CircleDetector
    {
        public double Param1;
        public double Param2;
        public int Exposure;
        public HoughModes DetectorType;

        public double zEstimate;                    //In mm to optical plane
        public CircleSegment CircleEstimate;        //In mm
        public OpenCvSharp.Rect ROI;                //In mm

        public CircleDetector(HoughModes mode, double param1, double param2, int exposure)
        {
            DetectorType = mode;
            Param1 = param1;
            Param2 = param2;
            Exposure = exposure;
        }
    }
}
