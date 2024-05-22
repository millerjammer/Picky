using System.Windows.Controls;
using System.Windows.Controls.Primitives;


namespace Picky
{
    /// <summary>
    /// Interaction logic for PickView.xaml
    /// </summary>
    public partial class PickToolView : UserControl
    {
        private readonly PickToolViewModel pickVM;

        public PickToolView(MachineModel mm)
        {
            InitializeComponent();
            pickVM = new PickToolViewModel(mm);
            this.DataContext = pickVM;
        }

        private void PickToolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PickListGrid.ScrollIntoView(PickListGrid.SelectedItem);
        }
    }
}
