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
    internal class PickListViewModel
    {
        public MachineModel Machine;

        public PickListViewModel()
        {
            Machine = MachineModel.Instance;
        }

        public ObservableCollection<Part> PickList
        {
            get { return Machine.PickList; }
            set
            {
                Machine.PickList = value; OnPropertyChanged(nameof(PickList));
            }
        }

        public Part selectedPickListPart
        {
            get { return Machine.selectedPickListPart; }
            set { Machine.selectedPickListPart = value; OnPropertyChanged(nameof(selectedPickListPart)); }
        }

        //private void SelectedPickListPart_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    OnPropertyChanged(nameof(selectedPickListPart));
        //}

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            fdr.width = Constants.FEEDER_THICKNESS;
            fdr.part = Machine.selectedPickListPart;
            Machine.selectedCassette.Feeders.Add(fdr);
            Machine.selectedPickListPart.cassette = Machine.selectedCassette;
        }

        public ICommand GoToPartLocationCommand { get { return new RelayCommand(GoToPartLocation); } }
        private void GoToPartLocation()
        {
            Machine.Messages.Add(GCommand.G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            Machine.Messages.Add(GCommand.G_GetPosition());
           // Machine.Messages.Add(GCommand.G_SetAbsoluteXYPosition(Machine.PCB_OriginX + (Convert.ToDouble(selectedPickListPart.CenterX) * Constants.MIL_TO_MM), Machine.PCB_OriginY + (Convert.ToDouble(selectedPickListPart.CenterY) * Constants.MIL_TO_MM)));
            Machine.Messages.Add(GCommand.G_GetPosition());
            Machine.Messages.Add(GCommand.G_SetAbsoluteZPosition(Constants.SAFE_TRANSIT_Z));
            Machine.Messages.Add(GCommand.G_GetPosition());
        }

        public ICommand OpenPickListCommand { get { return new RelayCommand(OpenPickList); } }
        private void OpenPickList()
        {
            bool isBody = false;
            int[] startIndex = new int[8];

            /* Remove existing, if one exists */
            Machine?.PickList.Clear();

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
