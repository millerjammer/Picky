using Microsoft.VisualStudio.Shell.Interop;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class Position3D : INotifyPropertyChanged
    {
        private double x;
        public double X
        {
            get { return x; }
            set { if (x != value) { x = value; OnPropertyChanged(nameof(X)); } }
        }

        private double y;
        public double Y
        {
            get { return y; }
            set { if (y != value) { y = value; OnPropertyChanged(nameof(Y)); } }
        }

        private double z;
        public double Z
        {
            get { return z; }
            set { if (z != value) { z = value; OnPropertyChanged(nameof(Z)); } }
        }

        private double width;
        public double Width
        {
            get { return width; }
            set { if (width != value) { width = value; OnPropertyChanged(nameof(Width)); } }
        }

        private double height;
        public double Height
        {
            get { return height; }
            set { if (height != value) { height = value; OnPropertyChanged(nameof(Height)); } }
        }

        private double angle;
        public double Angle
        {
            get { return angle; }
            set { if (angle != value) { angle = value; OnPropertyChanged(nameof(Angle)); } }
        }

        private double radius;
        public double Radius
        {
            get { return radius; }
            set { if (radius != value) { radius = value; OnPropertyChanged(nameof(Radius)); } }
        }

        private double quality;
        public double Quality
        {
            get { return quality; }
            set { if (quality != value) { quality = value; OnPropertyChanged(nameof(Quality)); } }
        }

        private double rotation = 0;
        public double Rotation
        {
            get { return rotation; }
            set { if (rotation != value) { rotation = value; OnPropertyChanged(nameof(Rotation)); } }
        }

        private bool isValid = false;
        public bool IsValid
        {
            get { return isValid; }
            set { if (isValid != value) { isValid = value; OnPropertyChanged(nameof(IsValid)); } }
        }

        public Position3D(double x, double y, double z, double angle)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Angle = angle;
        }

        public Position3D(double x, double y, double angle)
        {
            this.Z = x;
            this.Y = y;
            this.Angle = angle;
        }

        public Position3D(CircleSegment seg)
        {
            this.X = seg.Center.X; this.Y = seg.Center.Y;this.Radius = seg.Radius;
        }


        public Position3D() {
        }

        public OpenCvSharp.Rect GetRect()
        {
            return new OpenCvSharp.Rect((int)X, (int)Y, (int)Width, (int)Height);
        }

        public override string  ToString()
        {
            return string.Format("X:{0} Y:{1} Z:{2} Angle: {3} Rotation: {4} Radius: {5}", X, Y, Z, Angle, Rotation, Radius);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}
