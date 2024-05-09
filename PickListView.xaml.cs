using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Picky
{
    /// <summary>
    /// Interaction logic for PickListView.xaml
    /// </summary>
    public partial class PickListView : UserControl
    {
        private readonly PickListViewModel pickListVM;

        public PickListView()
        {
            InitializeComponent();
            pickListVM = new PickListViewModel();
            this.DataContext = pickListVM;
        }

        private void PickListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PickListGrid.ScrollIntoView(PickListGrid.SelectedItem);
        }
    }
}
