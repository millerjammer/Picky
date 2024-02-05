using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class MachineMessage
    {
        public enum MessageState { ReadyToSend, PendingOK, PendingPosition, Complete, Failed }

        public struct Pos{
            public double x;
            public double y;
            public double z;
            public double a;
            public double b;
            public byte axis;
        }

        public Pos target;
        public byte[] cmd;
        public MessageState state;
        public int timeout;
        public int delay;
        public Part part;
        public Feeder feeder;
      

        
        public MachineMessage() 
        {
            cmd = new byte[64];
            state = MessageState.ReadyToSend;
        }
    }
}
