using System;
using System.Windows.Controls;

namespace Picky
{
    /// <summary>
    /// Interaction logic for MachineView.xaml
    /// </summary>
    public partial class MachineView : UserControl
    {
        private readonly MachineViewModel machine;
        
        public MachineView(MachineModel mModel)
        {
            InitializeComponent();
            machine = new MachineViewModel(mModel);
            this.DataContext = machine;
        }
    }
}
