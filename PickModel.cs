using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class PickModel
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
        public double measured_width;

        /* Tool Storage, nominal and camera assisted actual */
        public double tool_storage_x { get; set; }
        public double tool_storage_y { get; set; }
        public double tool_storage_z { get; set; }

        public double actual_storage_x { get; set; }
        public double actual_storage_y { get; set; }
        public double actual_storage_z { get; set; }

        /* Physical Traits */
        public string Description { get; set; }

        public PickModel()
        {

        }

        public PickModel(string name)
        {
            Description = name;
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
