using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace Picky
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        MessageViewModel messageVM;
        public MessageWindow(MachineModel mm)
        {
            InitializeComponent();
           messageVM = new MessageViewModel(mm);
            this.DataContext = messageVM;
        }

        private void MachineMessageLoadingRow(object sender, DataGridRowEventArgs e)
        {
            //Select the last added row and scroll the window
            messageVM.selectedMachineMessage = messageVM.Messages.Last();
        }

        private void MachineMessageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MessageGrid.SelectedItem != null)
            {
                MessageGrid.ScrollIntoView(MessageGrid.SelectedItem);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Cancel the close operation
            e.Cancel = true;

            // Hide the window instead of closing it
            this.Hide();
        }
    }
}
