using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    internal class MessageViewModel : INotifyPropertyChanged
    {
        private MachineModel machine;

        /* Listen for changes on the machine properties and propagate to UI */
        //public MachineModel Machine
        //{
        //    get { return machine; }
        //}

        public MessageViewModel(MachineModel mm)
        {
            machine = mm;
            machine.Messages.CollectionChanged += OnCollectionChanged;
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
            Console.WriteLine("Property Change: " + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand OnLastCommand { get { return new RelayCommand(OnLast); } }
        private void OnLast()
        {
            machine.SelectedMachineMessage = machine.Messages.Last();
            OnPropertyChanged(nameof(selectedMachineMessage));

        }
    }
}
