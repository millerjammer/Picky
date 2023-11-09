using Microsoft.VisualStudio.OLE.Interop;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace Picky
{
    internal class CalibrationModel
    {

        /* Calibration Parameters - Pick Tool */
        private PickModel pickToolCal;
        public PickModel PickToolCal
        {
            get { return pickToolCal; }
            set { pickToolCal = value; }
        }

        /* Calibration Parameters - Scale */
        private OpenCvSharp.Rect refObject;
        public OpenCvSharp.Rect RefObject
        {
            get { return refObject; }
            set { Console.WriteLine("Reference Updated. Old: " + refObject.Width + " " + refObject.Height + " new: " + value.Width + " " + value.Height); refObject = value;  }
        }

        public double pCB_OriginX { get; set; }
        public double pCB_OriginY { get; set; }
        public double pCB_OriginZ { get; set; }

        public CalibrationModel() 
        {
            pickToolCal = new PickModel();
            refObject = new OpenCvSharp.Rect(0, 0, Constants.CALIBRATION_TARGET_WIDTH_DEFAULT_PIX, Constants.CALIBRATION_TARGET_HEIGHT_DEFAULT_PIX);
        }

        public void SaveCal()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String FullFileName = path + "\\" + Constants.CALIBRATION_FILE_NAME;
            File.WriteAllText(FullFileName, JsonConvert.SerializeObject(this, Formatting.Indented));
            Console.WriteLine("Calibration Data Saved.");
        }
    }
}
