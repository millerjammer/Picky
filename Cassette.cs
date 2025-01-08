using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Input;

namespace Picky
{
    public class Cassette : INotifyPropertyChanged, INotifyCollectionChanged
    {
        MachineModel machine = MachineModel.Instance;

        public static double CASSETTE_ORIGIN_X = 280.0;
        public static double CASSETTE_ORIGIN_Y = 280.0;

        public string FullFileName;

        private string _name = "untitled";
        public string name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(name)); }
        }
               
        private Position3D origin = new Position3D(CASSETTE_ORIGIN_X, CASSETTE_ORIGIN_Y, 0);
        public Position3D Origin
        {
            get { return origin; }
            set { origin = value; OnPropertyChanged(nameof(Origin)); }
        }

        private Feeder selectedFeeder;
        public Feeder SelectedFeeder
        {
            get { return selectedFeeder; }
            set { selectedFeeder = value; Console.WriteLine("Selected Feeder Set"); OnPropertyChanged(nameof(SelectedFeeder)); }
        }

        private ObservableCollection<Feeder> feeders;
        public ObservableCollection<Feeder> Feeders
        {
            get { return feeders; }
            set { feeders = value; OnPropertyChanged(nameof(Feeders)); }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFeederLocationsWithinCassette();
            Console.WriteLine("Feeder collection changed");
            OnPropertyChanged(nameof(Feeders)); // Notify that the collection has changed
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            Console.WriteLine("Cassette prop changed - " + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateFeederLocationsWithinCassette()
        {
            double x, offset = 0;
            for (int i = 0; i < feeders.Count; i++)
            {
                x = Origin.X + offset + (feeders.ElementAt(i).thickness/2);
                feeders.ElementAt(i).Origin = new Position3D(x, Origin.Y, 0, 0);
                offset += feeders.ElementAt(i).thickness;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Cassette()
        {
            feeders = new ObservableCollection<Feeder>();
            feeders.CollectionChanged += OnCollectionChanged;

        }

        public ICommand AddFeederCommand { get { return new RelayCommand(AddFeeder); } }
        public void AddFeeder()
        {
            Feeder feeder = new Feeder();
            feeder.Part = new Part();
            feeders.Add(feeder);
        }

        public ICommand GoToCassetteCommand { get { return new RelayCommand(GoToCassette); } }
        public void GoToCassette()
        {
            Console.WriteLine("Go To Cassette Position: " + Origin.X + " mm " + Origin.Y + " mm");
            machine.Messages.Add(GCommand.G_SetPosition(Origin.X, Origin.Y,0 ,0 ,0 ));
        }

        public ICommand SetCassetteHomeCommand { get { return new RelayCommand(SetCassetteHome); } }
        private void SetCassetteHome()
        {
            Origin = new Position3D(machine.CurrentX, machine.CurrentY);
            Console.WriteLine("Cassette Home: " + Origin.X + " mm " + Origin.Y + " mm");
            UpdateFeederLocationsWithinCassette();
        }

        public ICommand SaveAsCassetteCommand { get { return new RelayCommand(SaveAsCassette); } }
        private void SaveAsCassette()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".cst";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                FullFileName = saveFileDialog.FileName;
                name = saveFileDialog.SafeFileName;
                SaveCassette();
            }
        }

        public ICommand SaveCassetteCommand { get { return new RelayCommand(SaveCassette); } }
        private void SaveCassette()
        {
            /* Ignore a Parts reference to it's Cassette */
            File.WriteAllText(FullFileName, JsonConvert.SerializeObject(this, Formatting.Indented,
               new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    }
            ));
        }

        public ICommand CloseCassetteCommand { get { return new RelayCommand(CloseCassette); } }
        private void CloseCassette()
        {
            machine.Cassettes.Remove(this);
        }
    }
}
