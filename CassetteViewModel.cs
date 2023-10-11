using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public string myname { get; set; }
        public Part selectedPickListPart { get; set; }
        public Cassette selectedCassette { get; set; }

        private ObservableCollection<Cassette> cassettes;
        public ObservableCollection<Cassette> Cassettes
        {
            get { return cassettes; }
            set
            {
                cassettes = value;
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

        public CassetteViewModel()
        {
            pickList = new ObservableCollection<Part>();
            cassettes = new ObservableCollection<Cassette>();
  
        }

        public void Add()
        {
            Cassette cs = new Cassette();
            cs.name = "Untitled";
           Cassettes.Add(cs);
        
        }
             
        public event PropertyChangedEventHandler PropertyChanged;
    }
   
}
