using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;



namespace Picky
{
    /// <summary>
    /// Interaction logic for PickToolWindow.xaml
    /// </summary>
    public partial class PickToolWindow : Window
    {
        private readonly PickToolViewModel pickVM;

        public PickToolWindow()
        {
            InitializeComponent();
            pickVM = new PickToolViewModel();
            this.DataContext = pickVM;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Hide the window instead of closing it
            this.Hide();
        }

        private void PickToolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PickListGrid.ScrollIntoView(PickListGrid.SelectedItem);
        }
    }
}
