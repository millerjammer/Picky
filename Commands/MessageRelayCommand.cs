using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public interface MessageRelayCommand
    {
        bool PreMessageCommand(MachineMessage msg);
        bool PostMessageCommand(MachineMessage msg);
    }
}
