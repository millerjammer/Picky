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
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Xamarin.Forms.PlatformConfiguration.GTKSpecific;

namespace Picky
{
    public class Part : INotifyPropertyChanged
    {
        private string templateFileName;
        public string TemplateFileName
        {
            get { return templateFileName; }
            set { templateFileName = value; LoadTemplateImage(); OnPropertyChanged(nameof(TemplateFileName)); }
        }

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged(nameof(Description)); }
        }

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
        public string Thickness { get; set; }

        private Cassette cassette;
        public Cassette Cassette
        {
            get { return cassette; }
            set { cassette = value; OnPropertyChanged(nameof(Cassette)); }
        }

        private FeederModel feeder;
        public FeederModel Feeder
        {
            get { return feeder; }
            set { feeder = value; OnPropertyChanged(nameof(Feeder)); }
        }

        [JsonIgnore]
        private BitmapSource template;
        [JsonIgnore]
        public BitmapSource Template
        {
            get { return template; }
            set { template = value; OnPropertyChanged(nameof(Template)); }
        }

        [JsonIgnore]
        public Mat TemplateMat { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Part()
        {
            if(templateFileName == null)
                TemplateFileName = System.Environment.CurrentDirectory + "\\no_image_sm.png";
            if (string.IsNullOrEmpty(Description)) Description = string.Format("New Part");
        }

        public void LoadTemplateImage()
        {
            try
            {
                TemplateMat = Cv2.ImRead(TemplateFileName, ImreadModes.Color);
                Template = BitmapSource.Create(TemplateMat.Width, TemplateMat.Height, 96, 96, PixelFormats.Bgr24, null, TemplateMat.Data, (int)(TemplateMat.Step() * TemplateMat.Height), (int)TemplateMat.Step());
            }
            catch (Exception ex)
            {
                Console.WriteLine("No Template to load. ");
            }
        }
    }
}
