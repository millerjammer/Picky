using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    public class BoardViewModel
    {
        MachineModel machine = MachineModel.Instance;

        public BoardViewModel()
        {

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
            machine.Board.PcbOriginX = machine.CurrentX;
            machine.Board.PcbOriginY = machine.CurrentY;
        }
    }
}
