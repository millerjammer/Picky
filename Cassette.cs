using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Picky
{
    internal class Cassette : INotifyPropertyChanged
    {
        private string _name = "untitled";
        public string name
        {
            get { return _name; }
            set { _name = value; Console.WriteLine("namechange " + value);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("name"));
            }
        }
        public double x_origin { get; set; } = 0;
        public double y_origin { get; set; } = 0;
        public double z_origin { get; set; } = 0;

        public Feeder selectedFeeder { get; set; }

        private ObservableCollection<Feeder> feeders;
        public ObservableCollection<Feeder> Feeders
        {
            get { return feeders; }
            set
            {
                feeders = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Feeders"));
            }
        }

        public Cassette()
        {
            feeders = new ObservableCollection<Feeder>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
