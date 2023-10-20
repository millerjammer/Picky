﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Picky
{
    internal class CassetteViewModel : INotifyPropertyChanged
    {

        MachineModel machine = MachineModel.Instance; 
        
        public Part selectedPickListPart { get; set; }

        private Cassette _selectedCassette;
        public Cassette selectedCassette
        {
            get { return _selectedCassette; }
            set { _selectedCassette = value; PropertyChanged(this, new PropertyChangedEventArgs("selectedCassette")); }
        }

        public ObservableCollection<Cassette> Cassettes
        {
            get { return machine.Cassettes; }
            set
            {
                machine.Cassettes = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Cassettes"));
            }
        }

        private ObservableCollection<Part> pickList;
        public ObservableCollection<Part> PickList
        {
            get { return pickList; }
            set
            {
                pickList = value;
                PropertyChanged(this, new PropertyChangedEventArgs("PickList"));
            }
        } 
        
        public event PropertyChangedEventHandler PropertyChanged;

        public CassetteViewModel()
        {
            pickList = new ObservableCollection<Part>();

        }

        public void Add()
        {
            Cassette cs = new Cassette();
            Cassettes.Add(cs);
            selectedCassette = cs;
        }
             
       
    }
   
}
