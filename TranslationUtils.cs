using OpenCvSharp;
using Picky;
using System;
using System.Collections.Generic;

public static class TranslationUtils
{

    public static Position3D ConvertFrameRectPosPixToGlobalMM(Position3D pos_pix, double z)
    {
        /*------------------------------------------------------------------------------------
         * Returns a global Position3D in mm given a frame-centered Rectangle position.
         * It gets the center of the rectangle relative to the image center then adds an offset.
         * Requires x and y in pix and a z.
         * -----------------------------------------------------------------------------------*/

        MachineModel machine = MachineModel.Instance;
        
        var scale = machine.Cal.GetScaleMMPerPixAtZ(z);

        double x_center_pix = pos_pix.X + (pos_pix.Width / 2);
        double x_mm_offset = (x_center_pix - (Constants.CAMERA_FRAME_WIDTH / 2)) * scale.xScale;
        double x_mm_global = machine.Current.X - x_mm_offset;

        double y_center_pix = pos_pix.Y + (pos_pix.Height / 2);
        double y_mm_offset = (y_center_pix - (Constants.CAMERA_FRAME_HEIGHT / 2)) * scale.yScale;
        double y_mm_global = machine.Current.Y + y_mm_offset;
        
        return new Position3D(x_mm_global, y_mm_global, z, 0);
    }

    public static OpenCvSharp.Rect ConvertGlobalMMRectToFrameRectPix(Position3D global_rect_mm, double z)
    {
        /*------------------------------------------------------------------------------------
         * Returns the Rect, in pixels based on current position and passed Rect, in mm.
         * Requires the x and y scale in mm/pix - usually from a z.
         * -----------------------------------------------------------------------------------*/

        MachineModel machine = MachineModel.Instance;
        var scale = machine.Cal.GetScaleMMPerPixAtZ(z);

        double x_mm = machine.Current.X - global_rect_mm.X;
        double x_pix = x_mm / scale.xScale;
        int x = (x_pix > Constants.CAMERA_FRAME_WIDTH) ? 0 : (int)((Constants.CAMERA_FRAME_WIDTH / 2) + x_pix);

        double y_mm = machine.Current.Y - global_rect_mm.Y;
        double y_pix = y_mm / scale.yScale;
        int y = (y_pix > Constants.CAMERA_FRAME_HEIGHT) ? 0 : (int)((Constants.CAMERA_FRAME_HEIGHT / 2) - y_pix);

        if(x==0 && y==0)
            return default(OpenCvSharp.Rect);

        double width_mm = global_rect_mm.Width;
        double width_pix = width_mm / scale.xScale;
        int width = ((width_pix + x) > Constants.CAMERA_FRAME_WIDTH) ? Constants.CAMERA_FRAME_WIDTH - x : (int)width_pix;

        double height_mm = global_rect_mm.Height;
        double height_pix = height_mm / scale.yScale;
        int height = ((height_pix + y) > Constants.CAMERA_FRAME_HEIGHT) ? Constants.CAMERA_FRAME_HEIGHT - y : (int)height_pix;

        OpenCvSharp.Rect rect = new Rect(x, y, width, height);
        //Console.WriteLine("Global: " + global_rect_mm.ToString());
        //Console.WriteLine("ROI (px): " + rect.ToString());

        return rect;
    }

    public static double GetDistance(Position3D p1, Position3D p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy); // Euclidean distance formula
    }
}