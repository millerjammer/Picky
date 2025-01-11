using EnvDTE;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Picky
{
    /// <summary>
    /// Interaction logic for CassetteView.xaml
    /// </summary>
    public partial class CassetteView : UserControl
    {
        private readonly CassetteViewModel cassetteVM;
        
        public CassetteView()
        {
            InitializeComponent();
            cassetteVM = new CassetteViewModel();
            this.DataContext = cassetteVM;

        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            MachineModel machine = MachineModel.Instance;
            if(machine.SelectedCassette.SelectedFeeder.GoToFeederCommand.CanExecute(null))
                machine.SelectedCassette.SelectedFeeder.GoToFeederCommand.Execute(null);
        }
    }
}
