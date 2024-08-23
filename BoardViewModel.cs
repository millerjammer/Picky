using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace Picky
{
    public class BoardViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;

        private string _boardPlayPauseIcon;
        public string boardPlayPauseIcon
        {
            get { return _boardPlayPauseIcon; }
            set { _boardPlayPauseIcon = value; OnPropertyChanged(nameof(boardPlayPauseIcon)); }
        }

        private string _boardPlayPauseText;
        public string boardPlayPauseText
        {
            get { return _boardPlayPauseText; }
            set { _boardPlayPauseText = value; OnPropertyChanged(nameof(boardPlayPauseText)); }
        }

        public BoardViewModel()
        {
            boardPlayPauseText = "Place";
            boardPlayPauseIcon = Constants.PLAY_ICON;
        }
                
        public double PcbOriginX
        {
            get { return machine.Board.PcbOriginX; }
            set { machine.Board.PcbOriginX = value; OnPropertyChanged(nameof(PcbOriginX)); }
        }
                
        public double PcbOriginY
        {
            get { return machine.Board.PcbOriginY; }
            set { machine.Board.PcbOriginY = value; OnPropertyChanged(nameof(PcbOriginY)); }
        }
               
        public double PcbThickness
        {
            get { return machine.Board.PcbThickness; }
            set { machine.Board.PcbThickness = value; OnPropertyChanged(nameof(PcbThickness)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand GoToPCBOriginCommand { get { return new RelayCommand(GoToPCBOrigin); } }
        private void GoToPCBOrigin()
        {
            Console.WriteLine("GoTo PCB");
            machine.Messages.Add(GCommand.G_SetPosition(machine.Board.PcbOriginX, machine.Board.PcbOriginY, 0, 0, 0));
        }

        public ICommand SetAsPCBOriginCommand { get { return new RelayCommand(SetAsPCBOrigin); } }
        private void SetAsPCBOrigin()
        {
            Console.WriteLine("Set As PCB Origin");
            PcbOriginX = machine.CurrentX;
            PcbOriginY = machine.CurrentY;
            SaveBoard();
        }

        public ICommand OnBoardPlayPauseCommand { get { return new RelayCommand(OnBoardPlayPause); } }
        private void OnBoardPlayPause()
        {
            if (boardPlayPauseText == "Place")
            {
                boardPlayPauseText = "Pause";
                boardPlayPauseIcon = Constants.PAUSE_ICON;
                machine.isMachinePaused = false;
            }
            else
            {
                boardPlayPauseText = "Place";
                boardPlayPauseIcon = Constants.PLAY_ICON;
                machine.isMachinePaused = true;
            }
        }
        public void SaveBoard()
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            File.WriteAllText(path + "\\" + Constants.BOARD_FILE_NAME, JsonConvert.SerializeObject(this, Formatting.Indented));
            Console.WriteLine("Board Data Saved.");

        }
    }
}
