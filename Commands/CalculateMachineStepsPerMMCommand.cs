using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Picky.Tools
{
    public class CalculateMachineStepsPerMMCommand : MessageRelayCommand
    /*------------------------------------------------------------------------------
    * UPDATES Steps Per MM 
    *-------------------------------------------------------------------------------*/
    {
        public MachineMessage msg;

        public CalculateMachineStepsPerMMCommand()
        {
            msg = new MachineMessage();
            msg.messageCommand = this;
            msg.cmd = Encoding.ASCII.GetBytes("J102 Calculate Machine Steps Per MM\n");
        }

        public MachineMessage GetMessage()
        {
            return msg;
        }

        public bool PreMessageCommand(MachineMessage msg)
        {
            return true;
        }

        public bool PostMessageCommand(MachineMessage msg)
        {
            MachineModel machine = MachineModel.Instance;
            machine.Cal.CalTarget.CalculateMachineStepsPerMM();
            return true;
        }
    }
}
