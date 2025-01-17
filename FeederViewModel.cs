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
    internal class FeederViewModel 
    {
        public MachineModel machine { get; set; }
        private FeederModel feeder 
        {
            get { return machine?.SelectedCassette?.SelectedFeeder; }
        }
        
        public FeederViewModel()
        {
            machine = MachineModel.Instance;
        }
    }
}

