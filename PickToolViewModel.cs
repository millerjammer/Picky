﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    internal class PickToolViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        private MachineModel machine;

        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        public PickToolViewModel(MachineModel mm)
        {
            machine = mm;
            machine.PickToolList.CollectionChanged += OnCollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(PickToolList)); // Notify that the collection has changed (add/remove)
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PickToolModel selectedPickTool
        {
            get { return machine.SelectedPickTool; }
            set { Machine.SelectedPickTool = value; }
        }

        public ObservableCollection<PickToolModel> PickToolList
        {
            get { return Machine.PickToolList; }
            set { Machine.PickToolList = value; OnPropertyChanged(nameof(PickToolList)); }
        }

        public ICommand SaveToolsCommand { get { return new RelayCommand(SaveTools); } }
        private void SaveTools()
        {
            Machine.SaveTools();
        }

        public ICommand SetAsStorageLocationCommand { get { return new RelayCommand(SetAsStorageLocation); } }
        private void SetAsStorageLocation()
        {
            Machine.SelectedPickTool.ToolStorageX = Machine.CurrentX;
            Machine.SelectedPickTool.ToolStorageY = Machine.CurrentY;
            OnPropertyChanged(nameof(PickToolList)); // Notify that the collection has changed
        }

        public ICommand GoToStorageLocationCommand { get { return new RelayCommand(GoToStorageLocation); } }
        private void GoToStorageLocation()
        {
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
        }

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            //Move to Camera Position
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //TODO fine tune to center tool in window

            //Offset to Pick
            var offset = machine.Cal.GetPickHeadOffsetToCamera(Machine.SelectedPickTool.ToolStorageZ);
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.DownCameraToPickHeadX, machine.Cal.DownCameraToPickHeadY, 100, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            //Drive Z w/limit
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorageZ));
            //Open Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            //Retract Tool
            machine.Messages.Add(GCommand.G_SetPosition(machine.CurrentX, machine.CurrentY, 0, 0, 0));
            //Close Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //Camera to Tool (former) location
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(-machine.Cal.DownCameraToPickHeadX, -machine.Cal.DownCameraToPickHeadY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));


        }

        public event PropertyChangedEventHandler PropertyChanged;
        
    }
}
