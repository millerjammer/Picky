using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace Picky
{
    /// <summary>
    /// Interaction logic for CassetteView.xaml
    /// </summary>
    public partial class CassetteView : UserControl, INotifyCollectionChanged
    {
        private readonly CassetteViewModel cassette;

        public CassetteView()
        {
            InitializeComponent();

            cassette = new CassetteViewModel();
            this.DataContext = cassette;
            
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        void NewCassette(object sender, EventArgs e)
        {
            cassette.Add();
        }

        private void RemovePartFromCassette(object sender, RoutedEventArgs e)
        {
            cassette.selectedCassette.Feeders.Remove(cassette.selectedCassette.selectedFeeder);
        }

        private void AddPartToCassette(object sender, RoutedEventArgs e)
        {
            Feeder fdr = new Feeder();
            fdr.part = cassette.selectedPickListPart;
            cassette.selectedCassette.Feeders.Add(fdr);
        }

        private void CloseCassette(object sender, RoutedEventArgs e) 
        {
            cassette.Cassettes.Remove(cassette.selectedCassette);
        }

        private void SaveCassette(object sender, RoutedEventArgs e)
        {

        }

        private void LoadCassette(object sender, RoutedEventArgs e)
        {

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
