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
    /// Interaction logic for MachineView.xaml
    /// </summary>
    public partial class MachineView : UserControl
    {
        private readonly RelayInterface relayInterface;
        private readonly MachineViewModel machine;
        
        public MachineView()
        {
            InitializeComponent();

            relayInterface = new RelayInterface();
            machine = new MachineViewModel();
            this.DataContext = machine;
        }

        void ToggleLEDEvent(object sender, EventArgs e)
        {
            if ((bool)toggleLED.IsChecked)
                relayInterface.SetIlluminatorOn(true);
            else
                relayInterface.SetIlluminatorOn(false);

        }

        void TogglePumpEvent(object sender, EventArgs e)
        {
            if ((bool)togglePump.IsChecked)
                relayInterface.SetPumpOn(true);
            else
                relayInterface.SetPumpOn(false);

        }

        void ButtonXLeft(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeXYPosition(-machine.distanceToAdvance, 0));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonXRight(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeXYPosition(machine.distanceToAdvance, 0));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonYUp(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeXYPosition(0, machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonYDown(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeXYPosition(0, -machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonXYHome(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_Initialize());
            machine.messages.Enqueue(MachineCommands.S3G_FindXYMaximums());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }



        void ButtonZUp(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeZPosition(-machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonZHome(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_FindZMinimum());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonZDown(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeZPosition(machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonAUp(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeAngle(machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonAHome(object sender, EventArgs e)
        {

        }

        void ButtonADown(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeAngle(-machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonBUp(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeBPosition(machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }

        void ButtonBHome(object sender, EventArgs e)
        {

        }

        void ButtonBDown(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_SetRelativeBPosition(-machine.distanceToAdvance));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }
    }
}
