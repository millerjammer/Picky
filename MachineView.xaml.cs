using System.Windows.Controls;

namespace Picky
{
    /// <summary>
    /// Interaction logic for MachineView.xaml
    /// </summary>
    public partial class MachineView : UserControl
    {
        private readonly MachineViewModel machine;
        
        public MachineView()
        {
            InitializeComponent();
            machine = new MachineViewModel();
            this.DataContext = machine;
        }
    }
}
