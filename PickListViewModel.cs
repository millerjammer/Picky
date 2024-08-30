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
        private MachineModel Machine;

        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel machine
        {
            get { return Machine; }
        }

        public PickListViewModel()
        {
            Machine = MachineModel.Instance;
        }

        public Part selectedPickListPart
        {
            get { return Machine.selectedPickListPart; }
            set { Machine.selectedPickListPart = value; }
        }

        public ObservableCollection<Part> PickList
        {
            get { return Machine.PickList; }
            set { Machine.PickList = value; OnPropertyChanged(nameof(PickList));  }
        }
         

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
            fdr.width = Feeder.FEEDER_8MM_WIDTH_MILS;
            fdr.part = Machine.selectedPickListPart;
            Machine.selectedCassette.Feeders.Add(fdr);
            Machine.selectedPickListPart.cassette = Machine.selectedCassette;
        }

        public ICommand PickPlaceAllPartsCommand { get { return new RelayCommand(PickPlaceAllParts); } }
        private void PickPlaceAllParts()
        {
            Console.WriteLine("Pick-N-Place All");
            for (int i = 0; i < Machine.PickList.Count; i++)
            {
                machine.AddFeederPickToQueue(Machine.PickList.ElementAt(i).feeder);
                Machine.AddPartPlacementToQueue(Machine.PickList.ElementAt(i));
            }
        }

        public ICommand PickPlaceSinglePartCommand { get { return new RelayCommand(PickPlaceSinglePart); } }
        private void PickPlaceSinglePart()
        {
            Console.WriteLine("Pick-N-Place Single");
            machine.AddFeederPickToQueue(Machine.selectedPickListPart.feeder);
            Machine.AddPartPlacementToQueue(Machine.selectedPickListPart);
        }

        public ICommand PlaceSinglePartCommand { get { return new RelayCommand(PlaceSinglePart); } }
        private void PlaceSinglePart()
        {
            Machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            Machine.AddPartPlacementToQueue(Machine.selectedPickListPart);
        }
        
        public ICommand PlaceAllPartsCommand { get { return new RelayCommand(PlaceAllParts); } }
        private void PlaceAllParts()
        {
            Machine.Messages.Add(GCommand.SetCameraManualFocus(Machine.downCamera, true, Constants.FOCUS_PCB_062));
            Machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            Machine.Messages.Add(GCommand.G_SetZPosition(0));
            for (int i=0;i<Machine.PickList.Count;i++)
            {
                Machine.AddPartPlacementToQueue(Machine.PickList.ElementAt(i));
            }
        }

        public ICommand GoToPartLocationCommand { get { return new RelayCommand(GoToPartLocation); } }
        private void GoToPartLocation()
        {
            Machine.Messages.Add(GCommand.SetCameraManualFocus(Machine.downCamera, true, Constants.FOCUS_PCB_062));
            Machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            Machine.Messages.Add(GCommand.G_SetZPosition(0));
            double part_x = Machine.Board.PcbOriginX + (Convert.ToDouble(Machine.selectedPickListPart.CenterX) * Constants.MIL_TO_MM);
            double part_y = Machine.Board.PcbOriginY + (Convert.ToDouble(Machine.selectedPickListPart.CenterY) * Constants.MIL_TO_MM);
            Machine.Messages.Add(GCommand.G_SetPosition(part_x, part_y, 0, 0, 0));
        }

        public ICommand GoToAllPartLocationsCommand { get { return new RelayCommand(GoToAllPartLocations); } }
        private void GoToAllPartLocations()
        {
            Machine.Messages.Add(GCommand.SetCameraManualFocus(Machine.downCamera, true, Constants.FOCUS_PCB_062));
            Machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            Machine.Messages.Add(GCommand.G_SetZPosition(0));
            for (int i = 0; i < Machine.PickList.Count; i++)
            {
                Machine.AddPartLocationToQueue(Machine.PickList.ElementAt(i));
            }
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
