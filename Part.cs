using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Part : INotifyPropertyChanged
    {
        public string Designator { get; set; }
        public string Comment { get; set; }
        public string Layer { get; set; }
        public string Footprint { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string Rotation { get; set; }
        public string Description { get; set; }

        private string cassetteName;
        public string CassetteName 
        {
            get { return cassetteName; }
            set { cassetteName = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("cassetteName")); }
        }    

       
        public event PropertyChangedEventHandler PropertyChanged;

        
        public Part() { 
        }
    }
}
