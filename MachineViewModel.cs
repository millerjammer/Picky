using System;
using System.ComponentModel;
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
            get { int i = Array.IndexOf(DistToAdv, true); Console.WriteLine("Default: " + i); distanceToAdvance = distToAdvValue[i]; return DistToAdv; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MachineViewModel()
        {
                   
        }

        public ICommand ButtonXLeftCommand { get { return new RelayCommand(ButtonXLeft); } }
        private void ButtonXLeft()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition( distanceToAdvance, 0, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand ButtonXRightCommand { get { return new RelayCommand(ButtonXRight); } }
        private void ButtonXRight()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(-distanceToAdvance, 0, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand ButtonYUpCommand { get { return new RelayCommand(ButtonYUp); } }
        private void ButtonYUp()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, -distanceToAdvance, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand ButtonYDownCommand { get { return new RelayCommand(ButtonYDown); } }
        private void ButtonYDown()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, distanceToAdvance, 0, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));

        }

        public ICommand ButtonXYHomeCommand { get { return new RelayCommand(ButtonXYHome); } }
        private void ButtonXYHome()
        {
            machine.Messages.Add(GCommand.G_SetPosition(machine.CurrentX, machine.CurrentY, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FindMachineHome());
            
        }

        /**  Z-Axis *******************************/
        public ICommand ButtonZUpCommand { get { return new RelayCommand(ButtonZUp); } }
        private void ButtonZUp()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, -distanceToAdvance, 0, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));

        }

           
        public ICommand ButtonZDownCommand { get { return new RelayCommand(ButtonZDown); } }
        private void ButtonZDown()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_ProbeZ(+distanceToAdvance));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        /**  R-Axis *******************************/
        public ICommand ButtonRLeftCommand { get { return new RelayCommand(ButtonRLeft); } }
        private void ButtonRLeft()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, distanceToAdvance, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
        }

        public ICommand ButtonRRightCommand { get { return new RelayCommand(ButtonRRight); } }
        private void ButtonRRight()
        {
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(false));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, -distanceToAdvance, 0));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
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
