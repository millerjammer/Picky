﻿
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

        private bool isPreviewToolTargetActive = false;
        public bool IsPreviewToolTargetActive
        {
            get { return isPreviewToolTargetActive; }
            set
            {
                Machine.downCamera.IsTemplatePreviewActive = value;
                isPreviewToolTargetActive = value;
                if (value)
                {
                    Machine.SelectedPickTool.PreviewToolTemplate(machine.Cal.DeckPad);
                }
                OnPropertyChanged(nameof(IsPreviewToolTargetActive));
            }
        }

        public PickToolViewModel()
        {
            machine = MachineModel.Instance;
            machine.PickToolList.CollectionChanged += OnCollectionChanged;
            machine.SelectedPickTool = machine.PickToolList.FirstOrDefault();
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
                IsPreviewToolTargetActive = false;
                OnPropertyChanged(nameof(selectedPickTool));
            }
        }

        public ObservableCollection<PickToolModel> PickToolList
        {
            get { return Machine.PickToolList; }
            set { Machine.PickToolList = value; OnPropertyChanged(nameof(PickToolList)); }
        }

        
        public ICommand SetToolTemplateCommand { get { return new RelayCommand(SetToolTemplate); } }
        private void SetToolTemplate()
        {
            IsPreviewToolTargetActive = false;
            Machine.SelectedPickTool.SetToolTemplate();
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
            Machine.SelectedPickTool.ToolStorage.X = Machine.CurrentX;
            Machine.SelectedPickTool.ToolStorage.Y = Machine.CurrentY;
            Machine.SelectedPickTool.ToolStorage.Z = Constants.TOOL_NOMINAL_Z_DRIVE_MM;
            OnPropertyChanged(nameof(PickToolList)); // Notify that the collection has changed
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
               

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            
            Machine.SelectedPickTool.State = PickToolModel.TipStates.Loading;
            //Go to tool
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            //machine.Messages.Add(GCommand.SetCameraManualFocus(machine.downCamera, true, Constants.FOCUS_TOOL_RETRIVAL));
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorage.X + head.x_offset, Machine.SelectedPickTool.ToolStorage.Y - head.y_offset, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
                 //Align and adjust next position command
            //machine.Messages.Add(GCommand.OpticallyAlignToTool(Machine.SelectedPickTool));
            //machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            //machine.Messages.Add(GCommand.OffsetCameraToPick(0));
            //machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));

            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorage.Z));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //CalibrateTool();
            
        }

        public ICommand CalibrateToolLengthCommand { get { return new RelayCommand(CalibrateToolLength); } }
        private void CalibrateToolLength()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X, machine.Cal.CalPad.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.SetToolLength(machine.SelectedPickTool));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetZPosition(-2.0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X, machine.Cal.CalPad.Y, 0, 0, 0));
        }

        public ICommand CalibrateToolCommand { get { return new RelayCommand(CalibrateTool); } }
        private void CalibrateTool()
        {
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_EnableUpIlluminator(false));
            machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.CalPad.X, machine.Cal.CalPad.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.SetToolLength(machine.SelectedPickTool));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetZPosition(-2.0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            machine.Messages.Add(GCommand.G_FinishMoves());
            for (int i = 0; i < 360; i += 60)
            {
                machine.Messages.Add(GCommand.G_SetRotation(i));
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.SetToolOffsetCalibration(machine.SelectedPickTool, true));
            }
            //machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.ZCalDeckPadX, machine.Cal.ZCalDeckPadY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetZPosition(-2.0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            //machine.Messages.Add(GCommand.SetFocus(1000, machine.SelectedPickTool.LowerCircleDetector.Focus));
            machine.Messages.Add(GCommand.G_FinishMoves());
            for (int i = 0; i < 360; i += 60)
            {
                machine.Messages.Add(GCommand.G_SetRotation(i));
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.SetToolOffsetCalibration(machine.SelectedPickTool, false));
            }
            //machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.ZCalDeckPadX, machine.Cal.ZCalDeckPadY, 0, 0, 0));
        }

        public ICommand ReturnToolCommand { get { return new RelayCommand(ReturnTool); } }
        private void ReturnTool()
        {
            Machine.SelectedPickTool.State = PickToolModel.TipStates.Unloading;
            //Move to Camera Position
            //machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolReturnLocation.X, Machine.SelectedPickTool.ToolReturnLocation.Y, 0, 0, 0));
            //machine.Messages.Add(GCommand.OffsetCameraToPick(0));
            var head = machine.Cal.GetPickHeadOffsetToCameraAtZ(Constants.TOOL_NOMINAL_Z_DRIVE_MM);
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorage.X + head.x_offset, Machine.SelectedPickTool.ToolStorage.Y - head.y_offset, 0, 0, 0));
            
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_OpenToolStorage(true));
            machine.Messages.Add(GCommand.G_ProbeZ(Machine.SelectedPickTool.ToolStorage.Z));
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
