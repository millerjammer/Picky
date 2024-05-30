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

        private double pcbOriginX;
        public double PcbOriginX
        {
            get { return pcbOriginX; }
            set { pcbOriginX = value; OnPropertyChanged(nameof(PcbOriginX)); }

        }

        private double pcbOriginY;
        public double PcbOriginY
        {
            get { return pcbOriginY; }
            set { pcbOriginY = value; OnPropertyChanged(nameof(PcbOriginY)); }

        }

        private double pcbThickness;
        public double PcbThickness
        {
            get { return pcbThickness; }
            set { pcbThickness = value; OnPropertyChanged(nameof(PcbThickness)); }

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e));
        }
    }
}
