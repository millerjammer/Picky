using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Picky
{
    public class Cassette : INotifyPropertyChanged, INotifyCollectionChanged
    {
        MachineModel machine = MachineModel.Instance;

        private string _name = "untitled";
        public string name
        {
            get { return _name; }
            set { _name = value; Console.WriteLine("namechange " + value);
                if (PropertyChanged != null)
                    OnPropertyChanged(nameof(name));
            }
        }

        private double _x_origin = -265.56;
        public double x_origin
        {
            get { return _x_origin; }
            set { _x_origin = value; OnPropertyChanged(nameof(x_origin)); }
        }
        private double _y_origin = -118.24;
        public double y_origin
        {
            get { return _y_origin; }
            set { _y_origin = value; OnPropertyChanged(nameof(y_origin)); }
        }
        private double _z_origin = 0;
        public double z_origin
        {
            get { return _z_origin; }
            set { _z_origin = value; OnPropertyChanged(nameof(z_origin)); }
        }


        private Feeder _selectedFeeder;
        public Feeder selectedFeeder
        {
            get { return _selectedFeeder; }
            set { _selectedFeeder = value; OnPropertyChanged(nameof(selectedFeeder)); }
        }

        private ObservableCollection<Feeder> feeders;
        public ObservableCollection<Feeder> Feeders
        {
            get { return feeders; }
            set
            {
                feeders = value;
                OnPropertyChanged(nameof(Feeders));
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFeederLocationsWithinCassette();
            OnPropertyChanged(nameof(feeders)); // Notify that the collection has changed
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void UpdateFeederLocationsWithinCassette()
        {
            for (int i = 0; i < feeders.Count; i++)
            {
                feeders.ElementAt(i).z_origin = z_origin;
                feeders.ElementAt(i).y_origin = y_origin;
                feeders.ElementAt(i).x_origin = x_origin + ((Constants.FEEDER_THICKNESS * i) + Constants.FEEDER_INITIAL_XOFFSET);

                feeders.ElementAt(i).z_drive = z_origin;
                feeders.ElementAt(i).y_drive = y_origin - Constants.FEEDER_ORIGIN_TO_DRIVE_YOFFSET;
                feeders.ElementAt(i).x_drive = x_origin + ((Constants.FEEDER_THICKNESS * i) + Constants.FEEDER_INITIAL_XOFFSET);
            }
            Console.WriteLine("Updated " + feeders.Count + " feeders");
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Cassette()
        {
            feeders = new ObservableCollection<Feeder>();
            feeders.CollectionChanged += OnCollectionChanged;
        }

        public ICommand GoToCassetteCommand { get { return new RelayCommand(GoToCassette); } }
        private void GoToCassette()
        {
            Console.WriteLine("Go To Cassette Position: " + x_origin + " mm " + y_origin + " mm");
            machine.Messages.Add(Command.S3G_SetAbsoluteXYPosition(x_origin, y_origin));
            machine.Messages.Add(Command.S3G_GetPosition());
        }

        public ICommand SetCassetteHomeCommand { get { return new RelayCommand(SetCassetteHome); } }
        private void SetCassetteHome()
        {
            x_origin = machine.CurrentX;
            y_origin = machine.CurrentY;
            Console.WriteLine("Cassette Home: " + x_origin + " mm " + y_origin + " mm");
            UpdateFeederLocationsWithinCassette();
        }
    }
}
