using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Input;
using Xamarin.Forms;

namespace Picky
{
    internal class CassetteViewModel : INotifyPropertyChanged
    {
        public MachineModel Machine;

        public ObservableCollection<Cassette> Cassettes
        {
            get { return Machine.Cassettes; }
            set { Machine.Cassettes = value; OnPropertyChanged(nameof(Cassettes)); }
        }

        public Cassette selectedCassette
        {
            get { return Machine.SelectedCassette; } 
            set { Machine.SelectedCassette = value; if(Machine.SelectedCassette != null) Machine.SelectedCassette.PropertyChanged += SelectedCassette_PropertyChanged; OnPropertyChanged(nameof(selectedCassette)); }
        }

        private void SelectedCassette_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdatePickListCassetteReferences();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("collection change");
            UpdatePickListCassetteReferences();
            if (Machine.PickList != null && Machine.selectedPickListPart == null)
            {
                /* Make sure we're listening to changes on the selected Picklist */
                //Machine.PropertyChanged += SelectedPickListPart_PropertyChanged;
            }
        }

        public CassetteViewModel()
        {
            Machine = MachineModel.Instance;
            Machine.Cassettes.CollectionChanged += OnCollectionChanged;
        }
        
        public ICommand AddCassetteCommand { get { return new RelayCommand(AddCassette); } }
        private void AddCassette()
        {
            Cassette cs = new Cassette();
            Cassettes.Add(cs);
            selectedCassette = cs;
        }

        public void UpdatePickListCassetteReferences()
        {
            /* Update the Cassette references to each Part in the pickList */
            /* Occurs on cassetee or Feeder collection change Cassette i.e. add, remove */
            Console.WriteLine("Adding PickList Part Cassette References... ");
            foreach (Part part in Machine.PickList)
            {
                part.Cassette = null;
                
                foreach (Cassette cassette in Machine.Cassettes)
                {
                    foreach (Feeder feeder in cassette.Feeders)
                    {
                        if (part.Description == feeder.Part.Description && part.Footprint == feeder.Part.Footprint)
                        {
                            part.Cassette = cassette;
                            part.Feeder = feeder;
                        }
                    }
                }
            }
        }

        public ICommand LoadCassetteCommand { get { return new RelayCommand(LoadCassette); } }
        private void LoadCassette()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Cassette Files (*.cst, *.json)|*.cst;*.json";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                using (StreamReader file = File.OpenText(openFileDialog.FileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Machine.Cassettes.Add((Cassette)serializer.Deserialize(file, typeof(Cassette)));
                }
            }
            Machine.SelectedCassette = Machine.Cassettes.Last();
        }
    }
}
