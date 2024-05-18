using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class PickToolViewModel : INotifyPropertyChanged
    {
        public MachineModel machine;

        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
            get { return machine; }
        }

        public PickToolViewModel(MachineModel mm)
        {
            machine = mm;
            machine.PropertyChanged += OnMachinePropertyChanged;
        }

        public ObservableCollection<PickToolModel> PickToolList
        {
            get { return Machine.PickToolList; }
            set
            {
                Machine.PickToolList = value; OnCollectionChanged(nameof(PickToolList));
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
