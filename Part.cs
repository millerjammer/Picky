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
            set { template = value; Console.WriteLine("part template name changed"); OnPropertyChanged(nameof(TemplateFileName)); }
        }
        private double partDetectionThreshold = Constants.DEFAULT_PART_DETECTION_THRESHOLD;
        public double PartDetectionThreshold
        {
            get { return partDetectionThreshold; }
            set { partDetectionThreshold = value; OnPropertyChanged(nameof(PartDetectionThreshold)); }
        }
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

            template = System.Environment.CurrentDirectory + "\\no_image_sm.png";
        }
    }
}
