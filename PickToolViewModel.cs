
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            // Optically Correct location
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (Constants.CAMERA_FRAME_WIDTH / 10);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (Constants.CAMERA_FRAME_HEIGHT / 10);
            int width = 2 * (Constants.CAMERA_FRAME_WIDTH / 10);
            int height = 2 * (Constants.CAMERA_FRAME_WIDTH / 10);
            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, width, height);
            machine.Messages.Add(GCommand.C_LocateCircle(roi));
            machine.Messages.Add(GCommand.G_SetItemLocation());
            machine.Messages.Add(GCommand.G_EnableIlluminator(false));
        }

        public ICommand RetrieveToolCommand { get { return new RelayCommand(RetrieveTool); } }
        private void RetrieveTool()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            //Move to Camera Position
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //Offset to Pick
            var offset = machine.Cal.GetPickHeadOffsetToCamera(Machine.SelectedPickTool.ToolStorageZ);
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
            //Close Tool Caddy
            machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            //Camera to Tool (former) location
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            


        }

        public ICommand ReturnToolCommand { get { return new RelayCommand(ReturnTool); } }
        private void ReturnTool()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            //Move to Camera Position
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));
            //Offset to Pick
            var offset = machine.Cal.GetPickHeadOffsetToCamera(Machine.SelectedPickTool.ToolStorageZ);
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
            //Camera to Tool (former) location
            machine.Messages.Add(GCommand.G_SetPosition(Machine.SelectedPickTool.ToolStorageX, Machine.SelectedPickTool.ToolStorageY, 0, 0, 0));



        }

        public event PropertyChangedEventHandler PropertyChanged;
        
    }
}
