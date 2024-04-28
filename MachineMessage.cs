using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class MachineMessage
    {
        public enum MessageState { ReadyToSend, PendingOK, PendingPosition, Complete, Timeout, Failed }
        /*  
         *  ReadyToSend - The message has been created, is in the queue but has not been transmitted
         *  PendingOK   - The message has been sent but no 'ok' has been received, 
         *  PendingPosition - The message has been sent 'ok' has been received, but waiting for position to update (optional)
         *  Complete - The message has been sent, 'ok' has been recieved and processing is complete, ok to remove
         *  Timeout - A timeout occurred waiting for 'ok'.  Maybe resend
         *  Failed - An 'err' was recieved.  Maybe restart
         */

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
            delay = 1000;
            timeout = 5000;
        }
    }
}
