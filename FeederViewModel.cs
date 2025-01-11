using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Input;
using Xamarin.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Picky
{
    internal class FeederViewModel : INotifyPropertyChanged
    {
        public MachineModel machine { get; set; }

        /* Allow the dialog to make changes to the currently selected feeder
           If the selected feeder is changed the GUI will update */
        public MachineModel Machine
        {
            get { return machine; }
        }


        // Boilerplate for notifications
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public FeederViewModel()
        {
            machine = MachineModel.Instance;
        }

        
    }
}

