using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    public class BoardViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;

        public BoardViewModel()
        {

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
        }
    }
}
