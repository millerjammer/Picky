using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;

namespace Picky
{
    public class TipFitCalibration : INotifyPropertyChanged
    {

        private Position3D bestCircle; 
        public Position3D BestCircle
        {
            get { return bestCircle; }
            set { bestCircle = value; }
        }

        public TipFitCalibration()
        {
            BestCircle = new Position3D();
        }

        public Position3D CalculateBestFitCircle(List<Position3D> points)
        {
            int n = points.Count();

            if (n < 3)
            {
                throw new ArgumentException("At least 3 points are required to calculate a best-fit circle.");
            }

            double sumX = points.Sum(p => p.X);
            double sumY = points.Sum(p => p.Y);
            double sumX2 = points.Sum(p => p.X * p.X);
            double sumY2 = points.Sum(p => p.Y * p.Y);
            double sumXY = points.Sum(p => p.X * p.Y);
            double sumX3 = points.Sum(p => p.X * p.X * p.X);
            double sumY3 = points.Sum(p => p.Y * p.Y * p.Y);
            double sumXY2 = points.Sum(p => p.X * p.Y * p.Y);
            double sumX2Y = points.Sum(p => p.X * p.X * p.Y);

            double C = n * sumX2 - sumX * sumX;
            double D = n * sumXY - sumX * sumY;
            double E = n * sumY2 - sumY * sumY;
            double G = 0.5 * (n * sumX3 + n * sumXY2 - sumX * (sumX2 + sumY2));
            double H = 0.5 * (n * sumY3 + n * sumX2Y - sumY * (sumX2 + sumY2));

            double centerX = (E * G - D * H) / (C * E - D * D);
            double centerY = (C * H - D * G) / (C * E - D * D);
            double radius = Math.Sqrt((sumX2 + sumY2 - 2 * centerX * sumX - 2 * centerY * sumY) / n + centerX * centerX + centerY * centerY);

            // Calculate the fit quality (mean square error)
            double fitQuality = points.Average(p =>
            {
                double dx = p.X - centerX;
                double dy = p.Y - centerY;
                return Math.Pow(Math.Sqrt(dx * dx + dy * dy) - radius, 2);
            });

            // Convert from pixels to mm
            MachineModel machine = MachineModel.Instance;
            double z = points.Min(e => e.Z);
            var scale = machine.Cal.GetScaleMMPerPixAtZ(z);
            BestCircle.X = centerX * scale.xScale;
            BestCircle.Y = centerY * scale.yScale;
            BestCircle.Z = z;
            BestCircle.Radius = radius * scale.xScale;
            BestCircle.Quality = fitQuality;
            Position3D zeroPoint = points.OrderBy(p => p.Angle).First();
            // When we started calculating the calibration circle we know the pick angle.  But, 
            // We don't where on the calibration circle we are.  So, when we are done with the 
            // calibration circle calculations we'd like to know what angle on the calibration
            // circle corresponds to that initial zero angle pick location.
            BestCircle.Angle = GetAngleFromPoint(zeroPoint.X, zeroPoint.Y);
            Console.WriteLine("Cal Circle Offset: " + BestCircle.Angle + " @ " + zeroPoint.X + "," + zeroPoint.Y);
            foreach (Position3D point in points)
                Console.WriteLine("Calibration Circle: x, y: " + point.X + "," + point.Y + " @ " + point.Angle);
          
            return BestCircle;
        }
               

        // Method to calculate the angle of a point on the circle
        public double GetAngleFromPoint(double x, double y)
        {
            // Calculate the relative x and y positions from the circle's center
            double deltaX = x - BestCircle.X;
            double deltaY = y - BestCircle.Y;

            // Calculate the angle in radians
            double angleInRadians = Math.Atan2(deltaY, deltaX);

            // Convert to degrees
            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

            // Normalize angle to [0, 360)
            if (angleInDegrees < 0)
                angleInDegrees += 360;

            BestCircle.Angle = angleInDegrees;
            return angleInDegrees;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}
