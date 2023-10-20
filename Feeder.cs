using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Picky
{
    public class Feeder : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;

        public Part part { get; set; }
        public int index { get; set; }
        public int start_count { get; set; }
        public int placed_count { get; set; }
        public double width { get; set; }
        public double thickness { get; set; }
        public double interval { get; set; }

        public double x_origin { get; set; }
        public double y_origin { get; set; }
        public double z_origin { get; set; }

        public double x_drive { get; set; }
        public double y_drive { get; set; }
        public double z_drive { get; set; }

        
        public Feeder()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand GoToFeederCommand { get { return new RelayCommand(GoToFeeder); } }
        private void GoToFeeder()
        {
            Console.WriteLine("Go To Feeder Position: " + x_origin + " mm " + y_origin + " mm  Part: " + part.Description);
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_origin, y_origin));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand GoToFeederDriveCommand { get { return new RelayCommand(GoToFeederDrive); } }
        private void GoToFeederDrive()
        {
            Console.WriteLine("Go To Feeder Drive Position: " + x_drive + " mm " + y_drive + " mm");
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_drive, y_drive));
            machine.Messages.Add(Command.S3G_GetPosition());
        }
    }
}
