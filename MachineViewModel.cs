﻿using System;
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
            machine.Messages.Add(GCommand.G_GetPosition());
            machine.Messages.Add(GCommand.G_GetPosition());
            machine.relayInterface.SetIlluminatorOn(true);
            
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
            machine.Messages.Add(GCommand.G_SetZPosition(Constants.Z_AXIS_MAX));
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
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, +distanceToAdvance, 0, 0));
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

        public ICommand ButtonBUpCommand { get { return new RelayCommand(ButtonBUp); } }
        private void ButtonBUp()
        {
            machine.Messages.Add(GCommand.G_SetRelativeAngle(distanceToAdvance));
            machine.Messages.Add(GCommand.G_GetPosition());
        }

        public ICommand ButtonBHomeCommand { get { return new RelayCommand(ButtonBHome); } }
        private void ButtonBHome()
        {
            machine.Messages.Add(GCommand.G_SetAbsoluteAngle(0.00));
            machine.Messages.Add(GCommand.G_GetPosition());
        }
        public ICommand ButtonBDownCommand { get { return new RelayCommand(ButtonBDown); } }
        private void ButtonBDown()
        {
            machine.Messages.Add(GCommand.G_SetRelativeAngle(-distanceToAdvance));
            machine.Messages.Add(GCommand.G_GetPosition());
        }

        public ICommand ButtonAUpCommand { get { return new RelayCommand(ButtonAUp); } }
        private void ButtonAUp()
        {
            machine.Messages.Add(GCommand.G_SetRelativeAPosition(distanceToAdvance));
            machine.Messages.Add(GCommand.G_GetPosition());
        }

        public ICommand ButtonAHomeCommand { get { return new RelayCommand(ButtonAHome); } }
        void ButtonAHome()
        {

        }

        public ICommand ButtonADownCommand { get { return new RelayCommand(ButtonADown); } }
        private void ButtonADown()
        {
            machine.Messages.Add(GCommand.G_SetRelativeAPosition(-distanceToAdvance));
            machine.Messages.Add(GCommand.G_GetPosition());
        }

        public ICommand EditPickToolCommand { get { return new RelayCommand(EditPickTool); } }
        private void EditPickTool()
        {
            Console.WriteLine("Edit Pick Tool");
        }

        public ICommand GoToPCBOriginCommand { get { return new RelayCommand(GoToPCBOrigin); } }
        private void GoToPCBOrigin()
        {
            Console.WriteLine("GoTo PCB");
            machine.Messages.Add(GCommand.G_SetPosition(machine.PCB_OriginX, machine.PCB_OriginY, 0, 0, 0));
           
        }
        
        public ICommand SetAsPCBOriginCommand { get { return new RelayCommand(SetAsPCBOrigin); } }
        private void SetAsPCBOrigin()
        {
            Console.WriteLine("SetAs PCB Origin");
            machine.PCB_OriginX = machine.CurrentX;
            machine.PCB_OriginY = machine.CurrentY;
            machine.PCB_OriginZ = machine.CurrentZ;
            machine.SaveCalibrationSettings();

        }
    }
}
