﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Cassette : INotifyPropertyChanged
    {
        public string name { get; set; }
        public double x_origin { get; set; } = 0;
        public double y_origin { get; set; } = 0;
        public double z_origin { get; set; } = 0;

        private ObservableCollection<Feeder> feeders;
        public ObservableCollection<Feeder> Feeders
        {
            get { return feeders; }
            set
            {
                feeders = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("Feeders"));
            }
        }

        public Cassette()
        {
            feeders = new ObservableCollection<Feeder>();
           // name = "hhheehhe";
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
