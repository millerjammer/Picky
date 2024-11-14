using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class TipFitCalibration
    {
        
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
                double sumX2Y = points.Sum(p => p.X* p.X * p.Y);

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

                Position3D pos = new Position3D();
                pos.X = centerX;
                pos.Y = centerY;
                pos.Z = points.Min(e => e.Z);
                pos.Radius = radius;
                pos.Quality = fitQuality;
            
            return pos;
            }
        
    }
}
