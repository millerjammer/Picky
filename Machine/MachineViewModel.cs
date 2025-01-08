using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Picky
{

    public class MachineViewModel : INotifyPropertyChanged
    {
        MachineModel machine = MachineModel.Instance;
        
        /* Listen for changes on the machine properties and propagate to UI */
        public MachineModel Machine
        {
           get { return machine; }
           set { OnPropertyChanged(nameof(Machine)); }
        }
        

        /* For controls only */
        public double distanceToAdvance;
        private double[] distToAdvValue = new double[] { 0.1, 1.0, 10.0, 100.0 };
        private bool[] DistToAdv = new bool[] { false, true, false, false };
        public bool[] distToAdv
        {
            get { int i = Array.IndexOf(DistToAdv, true); distanceToAdvance = distToAdvValue[i]; return DistToAdv; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MachineViewModel()
        {
                   
        }

        public ICommand ButtonBLeftCommand { get { return new RelayCommand(ButtonBLeft); } }
        private void ButtonBLeft()
        {
            machine.Messages.Add(GCommand.G_DriveTapeAdvance((int)distanceToAdvance));
        }

        public ICommand ButtonBRightCommand { get { return new RelayCommand(ButtonBRight); } }
        private void ButtonBRight()
        {
            machine.Messages.Add(GCommand.G_DriveTapeAdvance((int)(-distanceToAdvance)));
        }

        public ICommand ButtonXLeftCommand { get { return new RelayCommand(ButtonXLeft); } }
        private void ButtonXLeft()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition( dest.x + distanceToAdvance, dest.y, dest_z, dest.a, dest.b));
        }

        public ICommand ButtonXRightCommand { get { return new RelayCommand(ButtonXRight); } }
        private void ButtonXRight()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition(dest.x - distanceToAdvance, dest.y, dest_z, dest.a, dest.b));
        }

        public ICommand ButtonYUpCommand { get { return new RelayCommand(ButtonYUp); } }
        private void ButtonYUp()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition(dest.x, dest.y - distanceToAdvance, dest_z, dest.a, dest.b));
        }

        public ICommand ButtonYDownCommand { get { return new RelayCommand(ButtonYDown); } }
        private void ButtonYDown()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition(dest.x, dest.y + distanceToAdvance, dest_z, dest.a, dest.b));
        }

        public ICommand ButtonXYHomeCommand { get { return new RelayCommand(ButtonXYHome); } }
        private void ButtonXYHome()
        {
            machine.Messages.Add(GCommand.G_SetZPosition(0));
            machine.Messages.Add(GCommand.G_FindMachineHome());
            
        }

        /**  Z-Axis *******************************/
        public ICommand ButtonZUpCommand { get { return new RelayCommand(ButtonZUp); } }
        private void ButtonZUp()
        {
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetZPosition(dest_z - distanceToAdvance));
            machine.Messages.Add(GCommand.G_EndstopStates());
        }
                   
        public ICommand ButtonZDownCommand { get { return new RelayCommand(ButtonZDown); } }
        private void ButtonZDown()
        {
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_ProbeZ(dest_z + distanceToAdvance));
            machine.Messages.Add(GCommand.G_EndstopStates());
        }

        /**  R-Axis *******************************/
        public ICommand ButtonRLeftCommand { get { return new RelayCommand(ButtonRLeft); } }
        private void ButtonRLeft()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition(dest.x, dest.y, dest_z, dest.a + distanceToAdvance, dest.b));
        }

        public ICommand ButtonRRightCommand { get { return new RelayCommand(ButtonRRight); } }
        private void ButtonRRight()
        {
            MachineMessage.Pos dest = machine.Messages.Last().target;
            double dest_z = machine.CurrentZ;   // Needed if following a triggered probe command
            machine.Messages.Add(GCommand.G_SetPosition(dest.x, dest.y, dest_z, dest.a - distanceToAdvance, dest.b));
        }

        /**  Toggle Switches *******************************/
        public ICommand ButtonLightCommand { get { return new RelayCommand(ButtonLight); } }
        private void ButtonLight()
        {
           if(machine.IsIlluminatorActive == true)
                machine.Messages.Add(GCommand.G_EnableIlluminator(false));
           else
                machine.Messages.Add(GCommand.G_EnableIlluminator(true));
        }

        public ICommand ButtonUpLightCommand { get { return new RelayCommand(ButtonUpLight); } }
        private void ButtonUpLight()
        {
            if (machine.IsUpIlluminatorActive == true)
                machine.Messages.Add(GCommand.G_EnableUpIlluminator(false));
            else
                machine.Messages.Add(GCommand.G_EnableUpIlluminator(true));
        }

        public ICommand ButtonPumpCommand { get { return new RelayCommand(ButtonPump); } }
        private void ButtonPump()
        {
            if (machine.IsPumpActive == true)
                machine.Messages.Add(GCommand.G_EnablePump(false));
            else
                machine.Messages.Add(GCommand.G_EnablePump(true));
        }

        public ICommand ButtonValveCommand { get { return new RelayCommand(ButtonValve); } }
        private void ButtonValve()
        {
            if (machine.IsValveActive == true)
                machine.Messages.Add(GCommand.G_EnableValve(false));
            else
                machine.Messages.Add(GCommand.G_EnableValve(true));
        }

        public ICommand ButtonOpenToolCommand { get { return new RelayCommand(ButtonTool); } }
        private void ButtonTool()
        {
            if (machine.IsToolStorageOpen == true)
                machine.Messages.Add(GCommand.G_OpenToolStorage(false));
            else
                machine.Messages.Add(GCommand.G_OpenToolStorage(true));
        }
    }
}
