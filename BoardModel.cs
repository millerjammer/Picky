using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class BoardModel : INotifyPropertyChanged
    {

        public double PcbOriginX { get; set; }
        public double PcbOriginY {  get; set; }
        public double PcbThickness { get; set; }
       
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}
