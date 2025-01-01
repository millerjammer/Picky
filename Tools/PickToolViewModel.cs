
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Dnn;
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
        public MachineModel machine { get; set; }
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        private bool isPreviewUpperToolTargetActive = false;     //This is the GUI button
        public bool IsPreviewUpperToolTargetActive
        {
            get { return isPreviewUpperToolTargetActive; }
            set
            {
                isPreviewUpperToolTargetActive = value;
                if (value)
                    Machine.SelectedPickTool.PreviewToolTemplate(machine.Cal.CalPad);
                OnPropertyChanged(nameof(IsPreviewUpperToolTargetActive));
            }
        }

        private bool isPreviewLowerToolTargetActive = false;     //This is the GUI button
        public bool IsPreviewLowerToolTargetActive
        {
            get { return isPreviewLowerToolTargetActive; }
            set
            {
                isPreviewLowerToolTargetActive = value;
                if (value)
                    Machine.SelectedPickTool.PreviewToolTemplate(machine.Cal.DeckPad);
                OnPropertyChanged(nameof(IsPreviewLowerToolTargetActive));
            }
        }

        public PickToolViewModel()
        {
            machine = MachineModel.Instance;
            machine.PickToolList.CollectionChanged += OnCollectionChanged;
            machine.SelectedPickTool = machine.PickToolList.FirstOrDefault();
            machine.SelectedPickTool.LoadImagery();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (machine.PickList.Count > 0)
            {
                machine.SelectedPickTool = machine.PickToolList.Last();
            }
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
                Console.WriteLine("Pick Tool Selection Changed");
                if (value != null)
                    value.LoadImagery();
                IsPreviewUpperToolTargetActive = false;
                IsPreviewLowerToolTargetActive = false;
                Machine.SelectedPickTool = value;
                OnPropertyChanged(nameof(selectedPickTool));
            }
        }

        public ObservableCollection<PickToolModel> PickToolList
        {
            get { return Machine.PickToolList; }
            set { Machine.PickToolList = value; OnPropertyChanged(nameof(PickToolList)); }
        }

        public ICommand SetUpperToolTemplateCommand { get { return new RelayCommand(SetUpperToolTemplate); } }
        public void SetUpperToolTemplate()
        {
            selectedPickTool.SaveCalPosition(selectedPickTool.UpperCal);
            selectedPickTool.UpperCal.LoadToolTemplateImage();
        }

        public ICommand SetLowerToolTemplateCommand { get { return new RelayCommand(SetLowerToolTemplate); } }
        public void SetLowerToolTemplate()
        {
            selectedPickTool.SaveCalPosition(selectedPickTool.LowerCal);
            selectedPickTool.LowerCal.LoadToolTemplateImage();
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
            if (PickToolList.Count() < Constants.TOOL_COUNT) {
                string name = string.Format("Tool{0}", PickToolList.Count());
                PickToolModel tool = new PickToolModel(name);
                tool.ToolStorage.X = Constants.TOOL_ORIGIN_X;
                tool.ToolStorage.Y = Constants.TOOL_ORIGIN_Y + (Constants.TOOL_ORIGIN_Y_PITCH * PickToolList.Count());
                tool.ToolStorage.Z = Constants.TOOL_ORIGIN_Z;
                machine.PickToolList.Add(tool);
                machine.SelectedPickTool = machine.PickToolList.Last();
            }
        }

        public ICommand SaveToolsCommand { get { return new RelayCommand(SaveTools); } }
        private void SaveTools()
        {
            Machine.SaveTools();
        }
        
        public ICommand SetAsStorageLocationCommand { get { return new RelayCommand(SetAsStorageLocation); } }
        private void SetAsStorageLocation()
        {
            if (Machine.SelectedPickTool != null)
            {
                Machine.SelectedPickTool.ToolStorage.X = Machine.CurrentX;
                Machine.SelectedPickTool.ToolStorage.Y = Machine.CurrentY;
                Machine.SelectedPickTool.ToolStorage.Z = Constants.TOOL_NOMINAL_Z_DRIVE_MM;
                OnPropertyChanged(nameof(PickToolList)); // Notify that the collection has changed
            }
        }

        public ICommand GoToStorageLocationCommand { get { return new RelayCommand(GoToStorageLocation); } }
        private void GoToStorageLocation()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorage.X, Machine.SelectedPickTool.ToolStorage.Y, 0, 0, 0));
        }
        
        public ICommand GoToUpperCalPadCommand { get { return new RelayCommand(GoToUpperCalPad); } }
        private void GoToUpperCalPad()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X, machine.Cal.CalPad.Y, 0, 0, 0));
        }

        public ICommand GoToLowerCalPadCommand { get { return new RelayCommand(GoToLowerCalPad); } }
        private void GoToLowerCalPad()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.DeckPad.X, machine.Cal.DeckPad.Y, 0, 0, 0));
        }

        public ICommand ProbePadCommand { get { return new RelayCommand(ProbePad); } }
        private void ProbePad()
        {
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetZPosition(-2.0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));

        }

        public ICommand AssignToSelectedFeederCommand { get { return new RelayCommand(assignToSelectedFeeder); } }
        private void assignToSelectedFeeder()
        {
            machine.selectedCassette.selectedFeeder.PickToolName = Machine.SelectedPickTool.UniqueID;
        }
        
        public ICommand MarkStateUnknownCommand { get { return new RelayCommand(markStateUnknown); } }
        private void markStateUnknown()
        {
            Machine.SelectedPickTool.State = PickToolModel.TipStates.Unknown;
        }

        public ICommand CalibrateToolCommand { get { return new RelayCommand(CalibrateToolTip); } }
        private void CalibrateToolTip()
        {
            Machine.SelectedPickTool.CalibrateTool();
        }

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            
            Machine.SelectedPickTool.State = PickToolModel.TipStates.Loading;
            //Go to tool
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorage.X + head.x_offset, Machine.SelectedPickTool.ToolStorage.Y - head.y_offset, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorage.Z));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //CalibrateTool();
            
        }

       
        public ICommand ReturnToolCommand { get { return new RelayCommand(ReturnTool); } }
        private void ReturnTool()
        {
            Machine.SelectedPickTool.State = PickToolModel.TipStates.Unloading;
            //Move to Camera Position
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorage.X + head.x_offset, Machine.SelectedPickTool.ToolStorage.Y - head.y_offset, 0, 0, 0));
            
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorage.Z));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
    }
}
