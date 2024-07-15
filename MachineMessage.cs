using Microsoft.VisualStudio.CommandBars;
using OpenCvSharp;
using System;
using System.CodeDom;
using System.ComponentModel;
using System.Linq;

namespace Picky
{
    public class MachineMessage : INotifyPropertyChanged
    {
        public enum MessageState { ReadyToSend, PendingDelay, PendingOK, PendingPosition, PendingImagery, Complete, Timeout, Aborted, Failed }
        /*  
         *  ReadyToSend - The message has been created, is in the queue but has not been transmitted
         *  PendingDelay - The message is waitig for it's delay to expire
         *  PendingOK   - The message has been sent but no 'ok' has been received, 
         *  PendingPosition - The message has been sent 'ok' has been received, but waiting for position to update (optional)
         *  PendingImagery - For camera messages, after request has been made, message will park here (optional)
         *  Complete - The message has been sent, 'ok' has been recieved and processing is complete, ok to remove
         *  Timeout - A timeout occurred waiting for 'ok'.  Maybe resend
         *  Aborted - Request places head out of bounds.  Command not executed, mostly for manual moves
         *  Failed - An 'err' was recieved.  Maybe restart
         */

        public struct Pos{
            public double x;
            public double y;
            public double z;
            public double a;
            public double b;
        }
        public int index { get; set; }
        public Pos target;
        public Rect roi;
        public int calType;
        public int iterationCount;
        public CircleSegment circleToFind;

        private Circle3d _bestLocation;
        public Circle3d bestLocation
        {
            get { return _bestLocation; }
            set { _bestLocation = value; OnPropertyChanged(nameof(bestLocation)); }
        }

        public void SetBestLocation(CircleSegment cir)
        {
            bestLocation.Radius = cir.Radius;
            bestLocation.X = cir.Center.X;
            bestLocation.Y = cir.Center.Y;
            
        }

                      
        private byte[] _cmd;
        public byte[] cmd 
        {
            get { return _cmd; }
            set { _cmd = value; cmdString = System.Text.Encoding.ASCII.GetString(_cmd); cmdString = cmdString.Substring(0, cmdString.Length - 1); }
        }
        private string _cmdString;
        public string cmdString
        {
            get { return _cmdString; }
            set { _cmdString = value; OnPropertyChanged(nameof(cmdString)); }
        }
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
            calType = 0x00;
            iterationCount = 1;
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
