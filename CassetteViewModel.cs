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
            get { return Machine.selectedCassette; }
            set { Machine.selectedCassette = value; OnPropertyChanged(nameof(selectedCassette)); }
        }

        public ObservableCollection<Part> PickList
        {
            get { return Machine.PickList; }
            set { Machine.PickList = value; OnPropertyChanged(nameof(PickList));
}       }

        public Part selectedPickListPart
        {
            get { return Machine.selectedPickListPart; }
            set { Machine.selectedPickListPart = value; OnPropertyChanged(nameof(selectedPickListPart)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            Console.WriteLine("property change: " + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

      
    private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("collection change"); 
            // Handle collection changes here
            // This method will be called when MyCollection changes
            // You can perform any necessary actions or notify other components about the changes.

        }

        public CassetteViewModel()
        {
            Machine = MachineModel.Instance;
            Machine.Cassettes.CollectionChanged += OnCollectionChanged;
            Machine.PickList.CollectionChanged += OnCollectionChanged;
        }
         
        public ICommand AddCassetteCommand { get { return new RelayCommand(AddCassette); } }
        private void AddCassette()
        {
            Cassette cs = new Cassette();
            Cassettes.Add(cs);
            selectedCassette = cs;
        }

        public ICommand AddPartToCassetteCommand { get { return new RelayCommand(AddPartToCassette); } }
        private void AddPartToCassette()
        {
            if (Machine.selectedPickListPart.cassette != null)
            {
                Console.WriteLine("Error, part already assigned");
                return;
            }
            if (Machine.selectedCassette == null)
            {
                Console.WriteLine("Error, no cassette selected");
                return;
            }
            Feeder fdr = new Feeder();
            fdr.part = Machine.selectedPickListPart;
            Console.WriteLine("Adding part to feeder... ");
            Machine.selectedCassette.Feeders.Add(fdr);
            Machine.selectedPickListPart.cassette = Machine.selectedCassette;
            /* Update the references in the pickList */
            foreach (Part part in Machine.PickList)
            {
                if (part.cassette == null)
                {
                    foreach (Feeder feeder in Machine.selectedCassette.Feeders)
                    {
                        if (part.Description == feeder.part.Description && part.Footprint == feeder.part.Footprint)
                        {
                            part.cassette = Machine.selectedCassette;
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
            Machine.selectedCassette = Machine.Cassettes.Last();
            /* Update the references in the pickList */
            foreach (Part part in Machine.PickList)
            {
                if (part.cassette == null)
                {
                    foreach (Feeder feeder in  Machine.selectedCassette.Feeders)
                    {
                        if (part.Description == feeder.part.Description && part.Footprint == feeder.part.Footprint)
                        {
                            part.cassette = Machine.selectedCassette;
                        }
                    }
                }
            }
        }

        public ICommand OpenPickListCommand { get { return new RelayCommand(OpenPickList); } }
        private void OpenPickList()
        {
            bool isBody = false;
            int[] startIndex = new int[8];
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Pick And Place Files (*.pnp, *.txt)|*.pnp;*.txt";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(openFileDialog.FileName);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!isBody)
                    {
                        if (lines[i].IndexOf("Designator") == 0)
                        {
                            startIndex[0] = 0;
                            startIndex[1] = lines[i].IndexOf("Comment");
                            startIndex[2] = lines[i].IndexOf("Layer");
                            startIndex[3] = lines[i].IndexOf("Footprint");
                            startIndex[4] = lines[i].IndexOf("Center-X");
                            startIndex[5] = lines[i].IndexOf("Center-Y");
                            startIndex[6] = lines[i].IndexOf("Rotation");
                            startIndex[7] = lines[i].IndexOf("Description");
                            isBody = true;
                        }
                    }
                    else
                    {
                        Part part = new Part();
                        part.Designator = lines[i].Substring(startIndex[0], lines[i].IndexOf(' ', startIndex[0]) - startIndex[0]);
                        if (lines[i].IndexOf('\"', startIndex[1], 1) >= 0)
                            part.Comment = lines[i].Substring(startIndex[1] + 1, lines[i].IndexOf('\"', startIndex[1] + 1) - startIndex[1] - 1);
                        else
                            part.Comment = lines[i].Substring(startIndex[1], lines[i].IndexOf(' ', startIndex[1]) - startIndex[1]);
                        part.Layer = lines[i].Substring(startIndex[2], lines[i].IndexOf(' ', startIndex[2]) - startIndex[2]);
                        part.Footprint = lines[i].Substring(startIndex[3], lines[i].IndexOf(' ', startIndex[3]) - startIndex[3]);
                        part.CenterX = lines[i].Substring(startIndex[4], lines[i].IndexOf(' ', startIndex[4]) - startIndex[4]);
                        part.CenterY = lines[i].Substring(startIndex[5], lines[i].IndexOf(' ', startIndex[5]) - startIndex[5]);
                        part.Rotation = lines[i].Substring(startIndex[6], lines[i].IndexOf(' ', startIndex[6]) - startIndex[6]);
                        part.Description = lines[i].Substring(startIndex[7] + 1, lines[i].IndexOf('\"', startIndex[7] + 1) - startIndex[7] - 1);

                        Console.WriteLine("Part: " + part.Description);
                        Machine.PickList.Add(part);
                    }
                }
                return;
            }
        }
    }
}
