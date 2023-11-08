using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class MachineMessage
    {
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
        public bool isPending;
        public int timeout;
        public int delay;
        public Part part;
        public Feeder feeder;
      

        
        public MachineMessage() 
        {
            cmd = new byte[64];
            isPending = false;
        }
    }
}
