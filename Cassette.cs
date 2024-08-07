﻿using Microsoft.VisualStudio.TextManager.Interop;
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

        public string FullFileName;

        private string _name = "untitled";
        public string name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(name)); }
        }

        private double _x_origin = Constants.CASSETTE_ORIGIN_X;
        public double x_origin
        {
            get { return _x_origin; }
            set { _x_origin = value; OnPropertyChanged(nameof(x_origin)); }
        }
        private double _y_origin = Constants.CASSETTE_ORIGIN_Y;
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
            set { _selectedFeeder = value; Console.WriteLine("sel cassette feeder changed"); OnPropertyChanged(nameof(selectedFeeder)); }
        }

        private ObservableCollection<Feeder> feeders;
        public ObservableCollection<Feeder> Feeders
        {
            get { return feeders; }
            set
            {
                feeders = value;
                Console.WriteLine("sel cassette feeder changed");
                OnPropertyChanged(nameof(Feeders));
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFeederLocationsWithinCassette();
            Console.WriteLine("feeder collection changed");
            OnPropertyChanged(nameof(Feeders)); // Notify that the collection has changed
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            Console.WriteLine("cassette prop changed - " + propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateFeederLocationsWithinCassette()
        {
            double offset = 0;
            for (int i = 0; i < feeders.Count; i++)
            {
                feeders.ElementAt(i).z_origin = 0;
                feeders.ElementAt(i).y_origin = y_origin;
                feeders.ElementAt(i).x_origin = x_origin + offset + (feeders.ElementAt(i).thickness/2);
                offset += feeders.ElementAt(i).thickness;

                feeders.ElementAt(i).y_drive = feeders.ElementAt(i).y_origin + Feeder.FEEDER_ORIGIN_TO_DRIVE_YOFFSET_MM;
                feeders.ElementAt(i).x_drive = feeders.ElementAt(i).x_origin + Feeder.FEEDER_ORIGIN_TO_DRIVE_XOFFSET_MM;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Cassette()
        {
            feeders = new ObservableCollection<Feeder>();
            feeders.CollectionChanged += OnCollectionChanged;

        }

        public ICommand GoToCassetteCommand { get { return new RelayCommand(GoToCassette); } }
        public void GoToCassette()
        {
            Console.WriteLine("Go To Cassette Position: " + x_origin + " mm " + y_origin + " mm");
            machine.Messages.Add(GCommand.G_SetPosition(x_origin, y_origin,0 ,0 ,0 ));
        }

        public ICommand SetCassetteHomeCommand { get { return new RelayCommand(SetCassetteHome); } }
        private void SetCassetteHome()
        {
            x_origin = machine.CurrentX;
            y_origin = machine.CurrentY;
            Console.WriteLine("Cassette Home: " + x_origin + " mm " + y_origin + " mm");
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
            /* Ignore a Parts reference to it's cassette */
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
