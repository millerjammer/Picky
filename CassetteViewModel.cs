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
        public MachineModel machine;
       
        public ObservableCollection<Cassette> Cassettes
        {
            get { return machine.Cassettes; }
            set { machine.Cassettes = value; OnPropertyChanged(nameof(Cassettes)); }
        }

        public Cassette selectedCassette
        {
            get { return machine.SelectedCassette; } 
            set { machine.SelectedCassette = value; if(machine.SelectedCassette != null) machine.SelectedCassette.PropertyChanged += SelectedCassette_PropertyChanged; OnPropertyChanged(nameof(selectedCassette)); }
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
            if (machine.PickList != null && machine.selectedPickListPart == null)
            {
                /* Make sure we're listening to changes on the selected Picklist */
                //machine.PropertyChanged += SelectedPickListPart_PropertyChanged;
            }
        }

        public CassetteViewModel()
        {
            machine = MachineModel.Instance;
            machine.Cassettes.CollectionChanged += OnCollectionChanged;
        }
        
        public ICommand AddCassetteCommand { get { return new RelayCommand(AddCassette); } }
        private void AddCassette()
        {
            InputDialog inputDialog = new InputDialog("Scan/Enter CASSETTE QR Code:");
            if (inputDialog.ShowDialog() == true)
            {
                string userInput = inputDialog.InputText;
                Cassette cs = new Cassette();
                cs.QRCode = userInput;
                Cassettes.Add(cs);
                selectedCassette = cs;
            }
        }

        public void UpdatePickListCassetteReferences()
        {
            /* Update the Cassette references to each Part in the pickList */
            /* Occurs on cassetee or FeederModel collection change Cassette i.e. add, remove */
            Console.WriteLine("Adding PickList Part Cassette References... ");
            foreach (Part part in machine.PickList)
            {
                part.Cassette = null;
                
                foreach (Cassette cassette in machine.Cassettes)
                {
                    foreach (FeederModel feeder in cassette.Feeders)
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

        public ICommand CreateCassetteCommand { get { return new RelayCommand(CreateCassette); } }
        private void CreateCassette()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Constants.TRAVEL_LIMIT_X_MM, machine.Cal.QRRegion.Y + (machine.Cal.QRRegion.Height / 2), 0, 0, 0));

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
                    machine.Cassettes.Add((Cassette)serializer.Deserialize(file, typeof(Cassette)));
                }
            }
            machine.SelectedCassette = machine.Cassettes.Last();
        }
    }
}
