using EnvDTE80;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class MachineMessage : INotifyPropertyChanged
    {
        public enum MessageState { ReadyToSend, PendingDelay, PendingOK, PendingPosition, Complete, Timeout, Failed }
        /*  
         *  ReadyToSend - The message has been created, is in the queue but has not been transmitted
         *  PendingDelay - The message is waitig for it's delay to expire
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
        public int index { get; set; }
        public Pos target;
        private byte[] _cmd;
        public byte[] cmd {
            get { return _cmd; }
            set { _cmd = value; cmdString = System.Text.Encoding.ASCII.GetString(_cmd); cmdString = cmdString.Substring(0, cmdString.Length - 1); }
        }
        public string cmdString { get; set; }
        private MessageState _state;
        public MessageState state
        {
            get { return _state; }
            set 
            { 
                _state = value;
                if (_state == MessageState.Complete)
                {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    actual_duration =now.ToUnixTimeMilliseconds() - start_time;
                }
                OnPropertyChanged(nameof(state)); 
            }
        }
        public int timeout { get; set; }
        public int delay { get; set; }
        public long start_time { get; set; }
        private long _actual_duration;
        public long actual_duration { get { return _actual_duration; } set { _actual_duration = value; OnPropertyChanged(nameof(actual_duration)); } }
        public Part part { get; set; }
        public Feeder feeder { get; set; }



        public MachineMessage() 
        {
            cmd = new byte[64];
            state = MessageState.ReadyToSend;
            delay = 1000;
            timeout = 4000;
            
            MachineModel mm = MachineModel.Instance;
            index = mm.Messages.Count();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
