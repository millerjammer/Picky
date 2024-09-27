
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Configuration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Picky
{
    public class PickToolViewModel : INotifyPropertyChanged, INotifyCollectionChanged
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
            set
            {
                Machine.SelectedPickTool = value;
                Console.WriteLine("Pick Tool Selection Changed");
                OnPropertyChanged(nameof(selectedPickTool));
            }
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

        public ICommand AssignToSelectedFeederCommand { get { return new RelayCommand(assignToSelectedFeeder); } }
        private void assignToSelectedFeeder()
        {
            machine.selectedCassette.selectedFeeder.PickToolName = Machine.SelectedPickTool.UniqueID;
        }
        
        public ICommand MarkStateUnknownCommand { get { return new RelayCommand(markStateUnknown); } }
        private void markStateUnknown()
        {
            Machine.SelectedPickTool.TipState = PickToolModel.TipStates.Unknown;
        }

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            
            Machine.SelectedPickTool.TipState = PickToolModel.TipStates.Loading;
            //Go to tool
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            //machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, true, Constants.FOCUS_TOOL_RETRIVAL));
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX + head.x_offset, Machine.SelectedPickTool.ToolStorageY - head.y_offset, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
                 //Align and adjust next position command
            //machine.Messages.Add(GCommand.OpticallyAlignToTool(Machine.SelectedPickTool));
            //machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            //machine.Messages.Add(GCommand.OffsetCameraToPick(0));
            //machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));

            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorageZ));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            CalibrateTool();
            
        }

        public ICommand CalibrateToolCommand { get { return new RelayCommand(CalibrateTool); } }
        private void CalibrateTool()
        {
            machine.SelectedPickTool.ResetPickOffsetCalibrationData();

            machine.Messages.Add(GCommand.SetCameraManualFocus(machine.upCamera, true, Constants.FOCUS_TIP_CAL));
            machine.Messages.Add(GCommand.G_EnableIlluminator(false));
            machine.Messages.Add(GCommand.G_EnableUpIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.OriginToPickHeadX1, machine.Cal.OriginToPickHeadY1, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Get offset to tip at two elevations
            for (int z = 0; z <= 10; z += 10)
            {
                for (int i = 0; i < 360; i += 60)
                {
                    machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.OriginToPickHeadX1, machine.Cal.OriginToPickHeadY1, z, i, 0));
                    machine.Messages.Add(GCommand.G_FinishMoves());
                    machine.Messages.Add(GCommand.SetToolOffsetCalibration(machine, machine.SelectedPickTool));
                }
                machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.OriginToPickHeadX1, machine.Cal.OriginToPickHeadY1, 0, 0, 0));
                machine.Messages.Add(GCommand.G_FinishMoves());
            }
        }

        public ICommand ReturnToolCommand { get { return new RelayCommand(ReturnTool); } }
        private void ReturnTool()
        {
            Machine.SelectedPickTool.TipState = PickToolModel.TipStates.Unloading;
            //Move to Camera Position
            //machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolReturnLocation.X, Machine.SelectedPickTool.ToolReturnLocation.Y, 0, 0, 0));
            //machine.Messages.Add(GCommand.OffsetCameraToPick(0));
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX + head.x_offset, Machine.SelectedPickTool.ToolStorageY - head.y_offset, 0, 0, 0));
            
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorageZ));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            //Camera to Tool (former) location
            //machine.Messages.Add(GCommand.OpticallyAlignToTool(Machine.SelectedPickTool));
            //machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
    }
}
