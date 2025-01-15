using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.GTKSpecific;

namespace Picky.Tools
{
    public class PickToolCalPosition : INotifyPropertyChanged
    {

        private CameraSettings captureSettings;
        public CameraSettings CaptureSettings
        {
            get { return captureSettings; }
            set { captureSettings = value; OnPropertyChanged(nameof(CaptureSettings)); }
        }

        private Position3D tipPosition;  //In pixels and MM (for z only)
        public Position3D TipPosition
        {
            get { return tipPosition; }
            set { tipPosition = value; OnPropertyChanged(nameof(TipPosition)); }
        }

        private Position3D tipOffsetMM;  //In mm
        public Position3D TipOffsetMM
        {
            get { return tipOffsetMM; }
            set { tipOffsetMM = value; OnPropertyChanged(nameof(TipOffsetMM)); }
        }

        [JsonIgnore]
        private BitmapSource toolTemplateImage;
        [JsonIgnore]
        public BitmapSource ToolTemplateImage
        {
            get { return toolTemplateImage; }
            set { toolTemplateImage = value; OnPropertyChanged(nameof(ToolTemplateImage)); }
        }

        private string toolTemplateFileName;
        public string ToolTemplateFileName
        {
            get { return toolTemplateFileName; }
            set { toolTemplateFileName = value; OnPropertyChanged(nameof(ToolTemplateFileName)); }
        }

        /* Tool ROI for template - captured at specific location */
        public OpenCvSharp.Rect toolROI { get; set; }

        public PickToolCalPosition(int focus, int threshold)
        {
            CaptureSettings = new CameraSettings();
            CaptureSettings.Focus = focus; CaptureSettings.BinaryThreshold = threshold;
            tipOffsetMM = new Position3D();
            tipPosition = new Position3D();
        }

        public void SaveToolTemplateImage()
        {
            MachineModel machine = MachineModel.Instance;
            Cv2.ImWrite(ToolTemplateFileName, new Mat(machine.downCamera.ColorImage, toolROI));
            while (File.Open(ToolTemplateFileName, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { }
        }

        public void LoadToolTemplateImage()
        {
            try
            {
                Mat img = Cv2.ImRead(ToolTemplateFileName, ImreadModes.Color);
                if (TipPosition != null)
                    Cv2.Circle(img, (int)TipPosition.X, (int)TipPosition.Y, (int)TipPosition.Radius, Scalar.Red);
                ToolTemplateImage = BitmapSource.Create(img.Width, img.Height, 96, 96, PixelFormats.Bgr24, null, img.Data, (int)(img.Step() * img.Height), (int)img.Step());
            }
            catch (Exception ex)
            {
                Console.WriteLine("No Template to load. ");
            }

        }

        public Position3D Set3DToolTipFromToolMat(Mat dialatedMat, double tipZ)
        {
            /*-------------------------------------------------------------
             * This function returns the position of the tool tip (in pixels)
             * given a localized image.  Used as Part of tip calibration.
             * the returned position is a circle in pixels measured from the 
             * origin x,y of the givem Mat
             * ------------------------------------------------------------*/

            TipPosition.Z = tipZ;

            // Restrict tip search to tool ROI
            Mat mat = new Mat(dialatedMat, toolROI);
            CircleSegment[] circles = Cv2.HoughCircles(
                mat,
                HoughModes.GradientAlt,
                dp: 1,           // Inverse ratio of accumulator resolution
                minDist: 10,     // Minimum distance between circle centers
                param1: 100,     // Higher threshold for the Canny edge detector
                param2: 0.65,      // Accumulator threshold for circle detection
                minRadius: 5,    // Minimum circle radius (10px diameter → radius = 5px)
                maxRadius: 20     // Maximum circle radius
            );

            // Calculate the center of the image
            OpenCvSharp.Point centerOfMat = new OpenCvSharp.Point(mat.Width / 2, mat.Height / 2);

            // Create a list of circles ordered by their distance to the center
            List<(Position3D CircleCenter, double Distance, double Y)> circlesWithDistances = new List<(Position3D, double, double)>();
            foreach (var circle in circles)
            {
                Position3D circleCenter = new Position3D { X = circle.Center.X, Y = circle.Center.Y, Radius = circle.Radius };
                double distance = Math.Sqrt(Math.Pow(circleCenter.X - centerOfMat.X, 2) + Math.Pow(circleCenter.Y - centerOfMat.Y, 2));
                circlesWithDistances.Add((circleCenter, distance, circle.Center.Y));
            }

            // Sort the circles by distance to the center
            var sortedCircles = circlesWithDistances
                .OrderByDescending(c => c.Y)
                .Select(c => c.CircleCenter)
                .ToList();

            if (sortedCircles.Count > 0)
            {
                Console.WriteLine("Circle R: " + sortedCircles[0].Radius);
                TipPosition.X = sortedCircles[0].X; TipPosition.Y = sortedCircles[0].Y; TipPosition.Radius = sortedCircles[0].Radius;
                set3DToolTipFromCenterInMM();
                return TipPosition;
            }
            return null;
        }

        private Position3D set3DToolTipFromCenterInMM()
        {
            /****************************************************************************
             * Returns the 3D position of the tip in the camera view. Given the Z value   
             * this function returns the X, Y offset from the image center in mm.  Uses
             * mm/pixel at the request z and similar triangles at the respective z in 
             * mm/pic
             ***************************************************************************/

            MachineModel machine = MachineModel.Instance;
            var mm_per_pix = machine.Cal.GetScaleMMPerPixAtZ(TipPosition.Z);

            TipOffsetMM.X = mm_per_pix.xScale * ((Constants.CAMERA_FRAME_WIDTH / 2) - (toolROI.X + TipPosition.X));
            TipOffsetMM.Y = mm_per_pix.yScale * ((Constants.CAMERA_FRAME_HEIGHT / 2) - (toolROI.Y + TipPosition.Y));
            TipOffsetMM.Z = TipPosition.Z;
            /* This can be used to offset the tip offset in Y for imaging on the +y side */
            /* This is not applied to TipOffset.Y */
            TipOffsetMM.Radius = mm_per_pix.yScale * TipPosition.Radius;

            Console.WriteLine("Offset from center of camera frame: " + TipOffsetMM.ToString());
            return (TipOffsetMM);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}

