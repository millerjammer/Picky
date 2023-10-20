using System.Windows.Controls;

namespace Picky
{
    /// <summary>
    /// Interaction logic for ControlView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        private readonly ControlViewModel controls;
        
        public ControlView()
        {
            InitializeComponent();
            controls = new ControlViewModel();
            this.DataContext = controls;
        }
    }
}
