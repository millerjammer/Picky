using System;
using OpenCvSharp;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Input;
using OpenCvSharp.Detail;

namespace Picky
{
    public class Part : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;

        private string template;
        public string TemplateFileName
        {
            get { return template; }
            set { template = value; Console.WriteLine("part template name changed "); OnPropertyChanged(nameof(TemplateFileName)); }
        }
        private double partDetectionThreshold = Constants.DEFAULT_PART_DETECTION_THRESHOLD;
        public double PartDetectionThreshold
        {
            get { return partDetectionThreshold; }
            set { partDetectionThreshold = value; OnPropertyChanged(nameof(PartDetectionThreshold)); }
        }
        public double Corrected_X { get; set; }
        public double Corrected_Y { get; set; }
        public string Designator { get; set; }
        public string Comment { get; set; }
        public string Layer { get; set; }
        public string Footprint { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string Rotation { get; set; }
        public string Description { get; set; }

        private Cassette _cassette;
        public Cassette cassette
        {
            get { return _cassette; }
            set { _cassette = value; OnPropertyChanged(nameof(cassette)); }
        }

        [JsonIgnore]
        public Mat Template { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public Part()
        {
        }

        public ICommand FindPartCommand { get { return new RelayCommand(FindPart); } }
        private void FindPart()
        {
            Console.WriteLine("Find Part Template");
            // Reference https://stackoverflow.com/questions/23180630/using-opencv-matchtemplate-for-blister-pack-inspection

            Mat gref = new Mat();
            Mat gtpl = new Mat();
            Mat mres = new Mat();
            Mat res_32f;
            Mat template = new Mat(TemplateFileName);

           

            Cv2.CvtColor(machine.currentRawImage, gref, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(template, gtpl, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            const int low_canny = 12;  /* Lower Values capture more edges, default is 110*/
            //Cv2.Canny(gref, gref, low_canny, low_canny * 3);
            //Cv2.Canny(gtpl, gtpl, low_canny, low_canny * 3);

            /* Show gray images in window */
            //Cv2.ImShow("file", gref);
            //Cv2.ImShow("template", gtpl);

            res_32f = new Mat(machine.currentRawImage.Rows - template.Rows + 1, machine.currentRawImage.Cols - template.Cols + 1, MatType.CV_32FC1);
            Cv2.MatchTemplate(gref, gtpl, res_32f, TemplateMatchModes.CCoeffNormed);

            /* Show Result */
            res_32f.ConvertTo(mres, MatType.CV_8U, 255.0);
            Cv2.ImShow("result", mres);

            int size = ((template.Cols + template.Rows) / 4) * 2 + 1; //force size to be odd
            Cv2.AdaptiveThreshold(mres, mres, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, size, -64);
            Cv2.ImShow("result_thresh", mres);

            /* Show all matches */
            while (true)
            {
                double minval, maxval;
                Point minloc, maxloc;
                Cv2.MinMaxLoc(mres, out minval, out maxval, out minloc, out maxloc);

                if (maxval > 0)
                {
                    Cv2.Rectangle(machine.currentRawImage, maxloc, new OpenCvSharp.Point(maxloc.X + template.Cols, maxloc.Y + template.Rows), new OpenCvSharp.Scalar(0, 255, 0), 2);
                    Cv2.FloodFill(mres, maxloc, 0); //mark drawn blob
                }
                else
                    break;
            }

            /* Show Result */
            Cv2.ImShow("final", machine.currentRawImage);
        }
    }
}
