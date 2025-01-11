using OpenCvSharp;
using Picky;
using System;
using System.Collections.Generic;

public static class TranslationUtils
{

    public static OpenCvSharp.Rect ConvertGlobalMMRectToFrameRectPix(Position3D global_rect_mm, double z)
    {
        /*------------------------------------------------------------------------------------
         * Returns the Rect, in pixels based on current position and passed Rect, in mm.
         * Requires the x and y scale in mm/pix - usually from a z.
         * -----------------------------------------------------------------------------------*/

        MachineModel machine = MachineModel.Instance;
        var scale = machine.Cal.GetScaleMMPerPixAtZ(z);

        double x_mm = machine.CurrentX - global_rect_mm.X;
        double x_pix = x_mm / scale.xScale;
        int x = (x_pix > Constants.CAMERA_FRAME_WIDTH) ? 0 : (int)((Constants.CAMERA_FRAME_WIDTH / 2) + x_pix);

        double y_mm = machine.CurrentY - global_rect_mm.Y;
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
}