using System.Windows.Controls;
using System.Windows.Controls.Primitives;


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
            zSlider.Maximum = Constants.Z_AXIS_MAX;
        }

        public void SliderZ(object sender, DragCompletedEventArgs e)
        {
           
        }

        public void SliderR(object sender, DragCompletedEventArgs e)
        {
            
        }
    }
}
