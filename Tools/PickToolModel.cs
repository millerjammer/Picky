using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Windows.Controls.Primitives;
using OpenCvSharp.Flann;

namespace Picky
{
    public class PickToolModel : INotifyPropertyChanged
    {

        public enum TipStates { Unknown, Loading, Calibrating, Ready, Unloading, Stored, Error }

        /* Position in pixels from camera to pick location */
        public double x_min { get; set; }
        public double x_max { get; set; }
        public double h_min { get; set; }
        public double h_max { get; set; }
        
        /* Angles in degrees */
        public double x_min_angle { get; set; }
        public double x_max_angle { get; set; }
        public double h_min_angle { get; set; }
        public double h_max_angle { get; set; }
        
        /* This is the offset to the circle described above */
        public double x_offset_from_camera { get; set; }
        public double y_offset_from_camera { get; set; }

        /* Measured Properties */
        public double Length { get; set; }
        public double Diameter { get; set; }

        /* Tool Storage, nominal and camera assisted actual */
        private double toolStorageX;
        public double ToolStorageX
        {
            get { return toolStorageX; }
            set { toolStorageX = value; OnPropertyChanged(nameof(ToolStorageX)); }

        }

        private double toolStorageY;
        public double ToolStorageY
        {
            get { return toolStorageY; }
            set { toolStorageY = value; OnPropertyChanged(nameof(ToolStorageY)); }
            
        }

        private double toolStorageZ;
        public double ToolStorageZ
        {
            get { return toolStorageZ; }
            set { toolStorageZ = value; OnPropertyChanged(nameof(ToolStorageZ)); }

        }

        public Point2d ToolReturnLocation { get; set; } 

        private TipStates tipState;
        public TipStates TipState
        {
            get { return tipState; }
            set { tipState = value; OnPropertyChanged(nameof(TipState)); }
        }

        /* Create the data point used for tip calibration */
        public List<Polar> CalDataPoints = new List<Polar>();
        
        private Polar tipOffset;
        public Polar TipOffset
        {
            get { return tipOffset; }
            set { tipOffset = value; OnPropertyChanged(nameof(TipOffset)); }
        }
                

        /* Physical Traits */
        public string Description { get; set; }

        /* Physical Traits */
        public string Name { get; set; }

        public PickToolModel(string name)
        {
            Description = name;
            TipOffset = new Polar(0,0,0);
            TipState = TipStates.Unknown;
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool SetPickOffsetCalibrationData(Polar point)
        {
            /* Calculate PickOffset from Calibration Data */
            CalDataPoints.Add(point);
            if(CalDataPoints.Count >= 6) {
                TipFitCalibration fitter = new TipFitCalibration();
                TipOffset = fitter.CalculateBestFitCircle(CalDataPoints);
                TipState = TipStates.Ready;
                SaveCalibrationCircleToFile();
                OnPropertyChanged(nameof(TipOffset));           //Manually notify the GUI
            }
            return true;
        }

        public void SaveCalibrationCircleToFile()
        {
            int x, y, r;
            MachineModel machine = MachineModel.Instance;
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect((Constants.CAMERA_FRAME_WIDTH / 3), (Constants.CAMERA_FRAME_HEIGHT / 3), Constants.CAMERA_FRAME_WIDTH / 3, Constants.CAMERA_FRAME_HEIGHT / 3);
            Mat roiImage = new Mat(machine.upCamera.ColorImage, roi);
            Mat resImage = roiImage.Clone();
            var scale = machine.Cal.GetScaleMMPerPixAtZ(25);
            for (int i = 0; i < 6; i++)
            {
                // Draw the circle outline
                Polar item = CalDataPoints.ElementAt(i);
                x = (int)((resImage.Width / 2) + (item.x / scale.xScale));
                y = (int)((resImage.Height / 2) + (item.y / scale.yScale));
                r = (int)(item.radius / scale.xScale);
                Cv2.Circle(resImage, x, y, r, Scalar.Green, 2);
                // Draw the circle center
                Cv2.Circle(resImage, x, y, 3, Scalar.Red, 3);
            }
            // Draw Result Calibration
            x = (int)((resImage.Width / 2) + (TipOffset.x / scale.xScale));
            y = (int)((resImage.Height / 2) + (TipOffset.y / scale.yScale));
            r = (int)(TipOffset.radius / scale.xScale);
            Cv2.Circle(resImage, x, y, r, Scalar.Black, 1);
            // Draw the circle center
            Cv2.Circle(resImage, x, y, 3, Scalar.Red, 3);
            Cv2.ImWrite(Constants.CALIBRATION_TIP_FILE_NAME, resImage);
        }

        public void DisplayCalibrationCircle()
        {
           
            if (TipState != PickToolModel.TipStates.Ready)
                return;
            Mat mat = Cv2.ImRead(Constants.CALIBRATION_TIP_FILE_NAME);
            if (mat.Empty()) { 
                Console.WriteLine("Image not found or could not be loaded.");
                return;
            }
            Cv2.ImShow("Tool Tip Calibration Result", mat);
            Cv2.WaitKey(1);
        }

        public Tuple<double, double> GetPickOffsetAtRotation(double angle_in_deg)
        {
            /******************************************************************************************/
            /* Returns a x,y offset of the pick location to center of the camera view, in pixels      */

            double x_delta = (x_max - x_min) / 2;
            double x_fraction = (Math.Cos((Math.PI / 180) * (angle_in_deg - x_min_angle)));
            double x_offset = x_min + x_delta - (x_delta * x_fraction);

            double h_delta = (h_max - h_min) / 2;
            double h_fraction = (Math.Cos((Math.PI / 180) * (angle_in_deg - h_min_angle)));
            double h_offset = h_min + h_delta - (h_delta * h_fraction);
                        
            return (new Tuple<double, double>(x_offset, h_offset));
        }

        public void Initialize()
        {
            x_max = x_min_angle = x_max_angle = h_min = h_max = h_max_angle = h_min_angle = 0;
            x_min = Constants.CAMERA_FRAME_WIDTH;
            h_min = Constants.CAMERA_FRAME_HEIGHT;
        }
    }
}
