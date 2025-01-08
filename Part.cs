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
        private string templateFileName;
        public string TemplateFileName
        {
            get { return templateFileName; }
            set { templateFileName = value; SetPartMat(); OnPropertyChanged(nameof(TemplateFileName)); }
        }
        private CameraSettings CaptureSettings { get; set; } = new CameraSettings();
       
        private bool isInView = false;
        public bool IsInView
        {
            get { return isInView; }
            set { isInView = value; OnPropertyChanged(nameof(IsInView)); }
        }
        public string Designator { get; set; }
        public string Comment { get; set; }
        public string Layer { get; set; }
        public string Footprint { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string Rotation { get; set; }
        public string Description { get; set; }
        public string Thickness { get; set; }
      
        private Cassette cassette;
        public Cassette Cassette
        {
            get { return cassette; }
            set { cassette = value; OnPropertyChanged(nameof(Cassette)); }
        }
        
        private Feeder feeder;
        public Feeder Feeder
        {
            get { return feeder; }
            set { feeder = value; OnPropertyChanged(nameof(Feeder)); }
        }
        
        [JsonIgnore]
        public Mat Template;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Part()
        {
            if(templateFileName == null)
                TemplateFileName = System.Environment.CurrentDirectory + "\\no_image_sm.png";
        }

        private void SetPartMat()
        {
            Console.WriteLine("Part setting Mat: " + templateFileName);
            if (templateFileName == null)
                return;
            Template = new Mat(templateFileName);

        }
    }
}
