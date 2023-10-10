using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Picky
{
    internal class CassetteViewModel : INotifyPropertyChanged
    {
        public string myname { get; set; }
        private ObservableCollection<Cassette> cassettes;
        public ObservableCollection<Cassette> Cassettes
        {
            get { return cassettes; }
            set
            {
                cassettes = value;
                //PropertyChanged(this, new PropertyChangedEventArgs("Cassettes"));
            }
        }

        public CassetteViewModel()
        {
            
            cassettes = new ObservableCollection<Cassette>();
            myname = "vm";

            AddCommand = new Command<string>((key) =>
            {
                myname += key;
                Console.WriteLine("constructor for create command: ");
            });
        }

        public ICommand AddCommand { protected set; get; }


        public void Add()
        {
            Cassettes.Add(new Cassette());
            Console.WriteLine("Here");
        }
             
        public event PropertyChangedEventHandler PropertyChanged;
    }
   
}
