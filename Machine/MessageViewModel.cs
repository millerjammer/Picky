﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Picky
{
    internal class MessageViewModel : INotifyPropertyChanged
    {
        private MachineModel machine;
        
        private string _playPauseIcon;
        public string playPauseIcon
        {
            get { return _playPauseIcon; }
            set { _playPauseIcon = value; OnPropertyChanged(nameof(playPauseIcon)); }
        }

        private string _playPauseText;
        public string playPauseText
        {
            get { return _playPauseText; }
            set { _playPauseText = value; OnPropertyChanged(nameof(playPauseText)); }
        }
             

        public MessageViewModel(MachineModel mm)
        {
            machine = mm;
            machine.Messages.CollectionChanged += OnCollectionChanged;
            playPauseText = "Pause";
            playPauseIcon = Constants.PAUSE_ICON;
        }

        public MachineMessage selectedMachineMessage
        {
            get { return machine.SelectedMachineMessage; }
            set { machine.SelectedMachineMessage = value; OnPropertyChanged(nameof(selectedMachineMessage)); }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Messages)); // Notify that the collection has changed
        }

        public ObservableCollection<MachineMessage> Messages
        {
            get { return machine.Messages; }
            set { machine.Messages = value; OnPropertyChanged(nameof(machine.Messages));}
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            //Console.WriteLine("Property Change: " + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand OnLastCommand { get { return new RelayCommand(OnLast); } }
        private void OnLast()
        {
            machine.SelectedMachineMessage = machine.Messages.Last();
            OnPropertyChanged(nameof(selectedMachineMessage));

        }

        public ICommand OnNextCommand { get { return new RelayCommand(OnNext); } }
        private void OnNext()
        {
            if(machine.Messages.Count > 0)
            {
                machine.advanceNextMessage = true;
            }   
        }

        public ICommand OnClearAllCommand { get { return new RelayCommand(OnClearAll); } }
        private void OnClearAll()
        {
            machine.Messages.Clear();
            machine.upCamera.CancelAllRequests();
            machine.downCamera.CancelAllRequests();
            machine.RxMessageCount = 0;
        }

        public ICommand OnPlayPauseCommand { get { return new RelayCommand(OnPlayPause); } }
        private void OnPlayPause()
        {
            if (playPauseText == "Play") {
                playPauseText = "Pause";
                playPauseIcon = Constants.PAUSE_ICON;
                machine.isMachinePaused = false;
            }
            else
            {
                playPauseText = "Play";
                playPauseIcon = Constants.PLAY_ICON;
                machine.isMachinePaused = true;
            }
        }
    }
}
