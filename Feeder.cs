using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class Feeder : INotifyPropertyChanged
    {
        public Part part { get; set; }
        public int index { get; set; }
        public int start_count { get; set; }
        public int placed_count { get; set; }
        public double width { get; set; }
        public double thickness { get; set; }
        public double interval { get; set; }

        public Feeder()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
