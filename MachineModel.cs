using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class MachineModel 
    {
       
        /* Cassetts that are installed */
        public ObservableCollection<Cassette> Cassettes { get; set; }
        /* Current machine position */
        public double CurrentX { get; set; } = 0;
        public double CurrentY { get; set; } = 0;
        public double CurrentZ { get; set; } = 0;
        public double CurrentA { get; set; } = 0;
        public double CurrentB { get; set; } = 0;
        

        /* Relay States */
        public bool isIlluminatorActive { get; set; } = false;
        public bool isPumpActive { get; set; } = false;
        public bool isValveClosed { get; set; } = false;

        public MachineModel()
        {
            Cassettes = new ObservableCollection<Cassette>();
     
        }
                 
    }
        
}
