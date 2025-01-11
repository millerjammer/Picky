using OpenCvSharp;
using System;
using System.Collections.Generic;

public class QRCodeUtils
{
    public static Rect[] ConvertPointsToRects(Point2f[] CodePoints)
    {
        // Ensure the input array's length is a multiple of 4
        if(CodePoints.Length == 0)
            return null;

        if (CodePoints.Length % 4 != 0)
        {
            throw new ArgumentException("The number of points must be a multiple of 4.");
        }

        List<Rect> rects = new List<Rect>();

        for (int i = 0; i < CodePoints.Length; i += 4)
        {
            // Extract the four points for the current rectangle
            Point2f p1 = CodePoints[i];
            Point2f p2 = CodePoints[i + 1];
            Point2f p3 = CodePoints[i + 2];
            Point2f p4 = CodePoints[i + 3];

            // Calculate the top-left and bottom-right corners of the bounding box
            float minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
            float minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
            float maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
            float maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));

            // Create a Rect using the calculated dimensions
            Rect rect = new Rect((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
            rects.Add(rect);
        }

        return rects.ToArray();
    }
}
