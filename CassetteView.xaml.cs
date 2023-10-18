using EnvDTE;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace Picky
{
    /// <summary>
    /// Interaction logic for CassetteView.xaml
    /// </summary>
    public partial class CassetteView : UserControl
    {
        private readonly CassetteViewModel cassette;

        public CassetteView(MachineModel mModel)
        {
            InitializeComponent();
            cassette = new CassetteViewModel(mModel);
            this.DataContext = cassette;

        }       

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        void NewCassette(object sender, EventArgs e)
        {
            cassette.Add();
          
        }

        private void RemovePartFromCassette(object sender, RoutedEventArgs e)
        {
            foreach (Part part in cassette.PickList)
            {
                if (part.Equals(cassette.selectedCassette.selectedFeeder.part))
                   part.cassette = null;
            }
            cassette.selectedCassette.Feeders.Remove(cassette.selectedCassette.selectedFeeder);

        }

        private void AddPartToCassette(object sender, RoutedEventArgs e)
        {
            if(cassette.selectedPickListPart.cassette != null)
            {
                Console.WriteLine("Error, part already assigned");
                return;
            }
            if (cassette.selectedCassette == null)
            {
                Console.WriteLine("Error, no cassette selected");
                return;
            }
            Feeder fdr = new Feeder();
            fdr.part = cassette.selectedPickListPart;
            cassette.selectedCassette.Feeders.Add(fdr);
            cassette.selectedPickListPart.cassette = cassette.selectedCassette;
            /* Update the references in the pickList */
            foreach (Part part in cassette.PickList)
            {
                if (part.cassette == null)
                {
                    foreach (Feeder feeder in cassette.selectedCassette.Feeders)
                    {
                        if (part.Description == feeder.part.Description && part.Footprint == feeder.part.Footprint)
                        {
                            part.cassette = cassette.selectedCassette;
                        }
                    }
                }
            }
        }

        private void CloseCassette(object sender, RoutedEventArgs e)
        {
            foreach (Part part in cassette.PickList)
            {
                if (part.cassette != null)
                {
                    if (part.cassette.Equals(cassette.selectedCassette))
                        part.cassette = null;
                }
            }
            cassette.Cassettes.Remove(cassette.selectedCassette);
        }

        private void SaveCassette(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".cst";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                cassette.selectedCassette.name = saveFileDialog.SafeFileName;
                /* Ignore a Parts reference to it's cassette */
                File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(cassette.selectedCassette, Formatting.Indented,
                    new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    }
                ));
            }
        }

        private void LoadCassette(object sender, RoutedEventArgs e)
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
                    cassette.Cassettes.Add((Cassette)serializer.Deserialize(file, typeof(Cassette)));
                }
            }
            cassette.selectedCassette = cassette.Cassettes.Last();
            /* Update the references in the pickList */
            foreach (Part part in cassette.PickList)
            {
                if (part.cassette == null)
                {
                    foreach (Feeder feeder in cassette.selectedCassette.Feeders)
                    {
                        if (part.Description == feeder.part.Description && part.Footprint == feeder.part.Footprint)
                        {
                            part.cassette = cassette.selectedCassette;
                        }
                    }
                }
            }
        }

        private void OpenPickList(object sender, RoutedEventArgs e)
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
                cassette.PickList = new ObservableCollection<Part>();
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
                        cassette.PickList.Add(part);
                    }
                }
                return;
            }
        }
    }
}
