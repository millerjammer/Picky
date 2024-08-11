using System;
using System.ComponentModel;

namespace Picky
{
    public class PickToolModel : INotifyPropertyChanged
    {
    
        /* This is data for calibration circle */

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


        /* Physical Traits */
        public string Description { get; set; }

        /* Physical Traits */
        public string Name { get; set; }

        public PickToolModel()
        {
            ToolStorageZ = Constants.TOOL_NOMINAL_Z_DRIVE_MM;
        }

        public PickToolModel(string name)
        {
            Description = name;
        }

        /* Default Send Notification boilerplate - properties that notify use OnPropertyChanged */
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
