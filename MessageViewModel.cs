using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class MessageViewModel : INotifyPropertyChanged
    {
        public MachineModel machine;

        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        public MessageViewModel(MachineModel mm)
        {
            machine = mm;
            machine.PropertyChanged += OnMachinePropertyChanged;
        }
               
        public ObservableCollection<MachineMessage> Messages
        {
            get { return Machine.Messages; }
            set
            {
                Machine.Messages = value; OnCollectionChanged(nameof(Messages));
            }
        }

        /* This is called when machine z changes */
        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //OnPropertyChanged(nameof(Machine.CurrentZ));
            //calcPickHeadToCamera(Machine.CurrentZ);
        }

        private void OnCollectionChanged(string v)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
