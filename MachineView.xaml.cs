using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace Picky
{
    /// <summary>
    /// Interaction logic for MachineView.xaml
    /// </summary>
    public partial class MachineView : UserControl
    {
        private readonly MachineViewModel mvm;

        public MachineView()
        {
            InitializeComponent();
            mvm = new MachineViewModel();
            this.DataContext = mvm;
            zSlider.Maximum = 500;
        }

        public void SliderZ(object sender, DragCompletedEventArgs e)
        {
           
        }

        public void SliderR(object sender, DragCompletedEventArgs e)
        {
            
        }
    }
}
