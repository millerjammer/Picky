
using OpenCvSharp;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Configuration;
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

        public PickToolViewModel()
        {
            machine = MachineModel.Instance;
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

        public ICommand RemoveToolCommand { get { return new RelayCommand(RemoveTool); } }
        private void RemoveTool()
        {
            Console.WriteLine("Removing Tool: " + selectedPickTool.Description);
            machine.PickToolList.Remove(selectedPickTool);
        }

        public ICommand NewToolCommand { get { return new RelayCommand(NewTool); } }
        private void NewTool()
        {
            if(PickToolList.Count() < Constants.TOOL_COUNT)
                machine.PickToolList.Add(new PickToolModel("untitled"));
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
            Machine.SelectedPickTool.ToolStorageZ = Constants.TOOL_NOMINAL_Z_DRIVE_MM;
            OnPropertyChanged(nameof(PickToolList)); // Notify that the collection has changed
        }

        public ICommand GoToStorageLocationCommand { get { return new RelayCommand(GoToStorageLocation); } }
        private void GoToStorageLocation()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
        }

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            //Go to tool
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //Align and adjust next position command
            machine.Messages.Add(GCommand.OpticallyAlignToSelectedTool(machine));
            //Offset to Pick           
            var offset = machine.Cal.GetPickHeadOffsetToCameraAtZ(machine.SelectedPickTool.ToolStorageZ);
            double x = Machine.SelectedPickTool.ToolStorageX + offset.x_offset;
            double y = Machine.SelectedPickTool.ToolStorageY + offset.y_offset;
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            //Drive Z w/limit
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorageZ));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Open Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            //Retract Tool
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Close Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //Turn off down illuminator and turn up illuminator on
            machine.Messages.Add(GCommand.G_EnableIlluminator(false));
            MachineMessage msg = GCommand.G_EnableUpIlluminator(true);
            msg.delay = 2000;
            machine.Messages.Add(msg);
            //Camera to Tool (former) location
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //Tool to home for calibration
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Get offset to tip
            for (int i = 0; i < 360; i += 90)
            {
                machine.Messages.Add(GCommand.SetPickOffsetCalibration(machine));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, i, 0));
                machine.Messages.Add(GCommand.G_FinishMoves());
            }
            machine.Messages.Add(GCommand.G_EnableUpIlluminator(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
        }

        public ICommand ReturnToolCommand { get { return new RelayCommand(ReturnTool); } }
        private void ReturnTool()
        {
            //Move to Camera Position
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //Offset to Pick
            var offset = machine.Cal.GetPickHeadOffsetToCameraAtZ(Machine.SelectedPickTool.ToolStorageZ);
            double x = Machine.SelectedPickTool.ToolStorageX + offset.x_offset;
            double y = Machine.SelectedPickTool.ToolStorageY + offset.y_offset;
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            //Open Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            //Drive Z w/limit
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorageZ));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Close Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //Retract Tool
            machine.Messages.Add(GCommand.G_SetPosition(x, y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Camera to Tool (former) location
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
    }
}
