using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class TipFitCalibration
    {
        
            public Polar CalculateBestFitCircle(List<Polar> points)
            {
                int n = points.Count();

                if (n < 3)
                {
                    throw new ArgumentException("At least 3 points are required to calculate a best-fit circle.");
                }

                double sumX = points.Sum(p => p.x);
                double sumY = points.Sum(p => p.y);
                double sumX2 = points.Sum(p => p.x * p.x);
                double sumY2 = points.Sum(p => p.y * p.y);
                double sumXY = points.Sum(p => p.x * p.y);
                double sumX3 = points.Sum(p => p.x * p.x * p.x);
                double sumY3 = points.Sum(p => p.y * p.y * p.y);
                double sumXY2 = points.Sum(p => p.x * p.y * p.y);
                double sumX2Y = points.Sum(p => p.x* p.x * p.y);

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
                    double dx = p.x - centerX;
                    double dy = p.y - centerY;
                    return Math.Pow(Math.Sqrt(dx * dx + dy * dy) - radius, 2);
                });

            return new Polar
            {
                x = centerX,
                y = centerY,
                z = points.Min(e => e.z),
                radius = radius,
                quality = fitQuality
                };
            }
        
    }
}
