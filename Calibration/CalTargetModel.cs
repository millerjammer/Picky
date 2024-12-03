using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup;

namespace Picky
{
    public class CalTargetModel : INotifyPropertyChanged
    {
        /* Physical properties of the Target */
        public static double GRID_ORIGIN_X_MILS = 400;
        public static double GRID_ORIGIN_Y_MILS = 2900;
        public static double GRID_SPACING_X_MILS = 4000;
        public static double GRID_SPACING_Y_MILS = 4000;
        public static double GRID_MONUMENT_RADIUS_MM = 1.27;
        public static double GRID_MONUMENT_HEIGHT_MM = 1.778;

        public static double LOWER_GRID_X_MILS = 1200;
        public static double LOWER_GRID_Y_MILS = 4900;
        public static double LOWER_GRID_Z_OFFSET_FROM_DECK_MM = 1;
        
        public static double UPPER_GRID_X_MILS = 3600;
        public static double UPPER_GRID_Y_MILS = 4900;
        public static double UPPER_LOWER_GRID_Z_OFFSET_MM = 10;

        public static double OPTICAL_GRID_X_MM = 10;
        public static double OPTICAL_GRID_Y_MM = 10;
        public static double OPTICAL_GRID_RADIUS_MM = 1.5;

        /* Targets for Steps per MM */
        private Position3D actualLoc00;
        public Position3D ActualLoc00 
        {
            get { return actualLoc00; }
            set { actualLoc00 = value; OnPropertyChanged(nameof(ActualLoc00)); }
        }
        private Position3D actualLoc10;
        public Position3D ActualLoc10
        {
            get { return actualLoc10; }
            set { actualLoc10 = value; OnPropertyChanged(nameof(ActualLoc10)); }
        }
        private Position3D actualLoc01;
        public Position3D ActualLoc01
        {
            get { return actualLoc01; }
            set { actualLoc01 = value; OnPropertyChanged(nameof(ActualLoc01)); }
        }
        private Position3D actualLoc11;
        public Position3D ActualLoc11
        {
            get { return actualLoc11; }
            set { actualLoc11 = value; OnPropertyChanged(nameof(ActualLoc11)); }
        }

        /* Upper and Lower Resolution Targets */
        private Position3D actualLocLower;
        public Position3D ActualLocLower
        {
            get { return actualLocLower; }
            set { actualLocLower = value; OnPropertyChanged(nameof(ActualLocLower)); }
        }

        private Position3D actualLocUpper;
        public Position3D ActualLocUpper
        {
            get { return actualLocUpper; }
            set { actualLocUpper = value; OnPropertyChanged(nameof(ActualLocUpper)); }
        }

        private Position3D calCircle;
        public Position3D CalCircle
        {
            get { return calCircle; }
            set { calCircle = value; OnPropertyChanged(nameof(CalCircle)); }
        }

        private List<CircleSegment> upperGridCircles;
        private List<CircleSegment> lowerGridCircles;

        public CalTargetModel()
        {
         
        }

        public void CalibrateMMPerPixelAtZ()
        /*---------------------------------------------------------------------
         * Uses step alignment to determine mm/pix at a specific target. This
         * is a calibration used to enable jump step connections based on cammea
         * to target.  This function is performed at two different target z 
         * elevations.  This should be done after calibrating mm per steps.
         * Requires that Z is calibrated at the calibration pad
         * -------------------------------------------------------------------*/
        {
            MachineModel machine = MachineModel.Instance;
            double dist_to_deck_mm = machine.Cal.ZCalPadZ + Constants.ZOFFSET_CAL_PAD_TO_DECK;

            ActualLocUpper.Radius = GRID_MONUMENT_RADIUS_MM;
            ActualLocLower.Radius = GRID_MONUMENT_RADIUS_MM;
            ActualLocLower.Z = dist_to_deck_mm - LOWER_GRID_Z_OFFSET_FROM_DECK_MM;
            ActualLocUpper.Z = dist_to_deck_mm - UPPER_LOWER_GRID_Z_OFFSET_MM;

            /* Get Upper */
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocUpper.X, ActualLocUpper.Y, 0, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(ActualLocUpper));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            machine.Messages.Add(GCommand.GetGridCalibration(upperGridCircles, ActualLocUpper));

            /* Get Lower */
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocLower.X, ActualLocLower.Y, 0, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(ActualLocLower));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.GetGridCalibration(lowerGridCircles, ActualLocLower));

            machine.Messages.Add(GCommand.CalculateMMPerPixel());
        }

        private void SetStepsPerMMTargetDefaults()
        {
            ActualLoc00 = new Position3D((GRID_ORIGIN_X_MILS * Constants.MIL_TO_MM), (GRID_ORIGIN_Y_MILS * Constants.MIL_TO_MM), GRID_MONUMENT_HEIGHT_MM, GRID_MONUMENT_RADIUS_MM);
            ActualLoc10 = new Position3D((GRID_ORIGIN_X_MILS * Constants.MIL_TO_MM) + (GRID_SPACING_X_MILS * Constants.MIL_TO_MM), (GRID_ORIGIN_Y_MILS * Constants.MIL_TO_MM), GRID_MONUMENT_HEIGHT_MM, GRID_MONUMENT_RADIUS_MM);
            ActualLoc01 = new Position3D((GRID_ORIGIN_X_MILS * Constants.MIL_TO_MM), (GRID_ORIGIN_Y_MILS * Constants.MIL_TO_MM) + (GRID_SPACING_Y_MILS * Constants.MIL_TO_MM), GRID_MONUMENT_HEIGHT_MM, GRID_MONUMENT_RADIUS_MM);
            ActualLoc11 = new Position3D((GRID_ORIGIN_X_MILS * Constants.MIL_TO_MM) + (GRID_SPACING_X_MILS* Constants.MIL_TO_MM), (GRID_ORIGIN_Y_MILS* Constants.MIL_TO_MM) + (GRID_SPACING_Y_MILS* Constants.MIL_TO_MM), GRID_MONUMENT_HEIGHT_MM, GRID_MONUMENT_RADIUS_MM);
        }

        public void CalibrateMMPerStep()
        /*---------------------------------------------------------------------
         * Uses step alignment to determine mm/step. This
         * is a calibration used to verify or update settings in the stepper 
         * motor controller. This should be the first calibration that's done.
         *  - TODO Use a calibrated Z.  This will read current steps per mm.
         * -------------------------------------------------------------------*/
        {
            MachineModel machine = MachineModel.Instance;
            upperGridCircles = new List<CircleSegment>();
            lowerGridCircles = new List<CircleSegment>();


            // TODO Replace with calibrated Z
            ActualLoc00.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            ActualLoc00.Radius = GRID_MONUMENT_RADIUS_MM;
            ActualLoc01.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            ActualLoc01.Radius = GRID_MONUMENT_RADIUS_MM;
            ActualLoc10.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            ActualLoc10.Radius = GRID_MONUMENT_RADIUS_MM;
            ActualLoc11.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            ActualLoc11.Radius = GRID_MONUMENT_RADIUS_MM;

            // Update 01 and 10 defaults location from 11 and 00 positions set by the GUI
            ActualLoc01.X = ActualLoc00.X;
            ActualLoc01.Y = ActualLoc11.Y;
            ActualLoc10.X = ActualLoc11.X;
            ActualLoc10.Y = ActualLoc00.Y;

            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            // For the x = 0, y = 0 position
            machine.Messages.Add(GCommand.G_SetPosition(ActualLoc00.X, ActualLoc00.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(ActualLoc00));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            //For the x = 1, y = 1 position
            machine.Messages.Add(GCommand.G_SetPosition(ActualLoc11.X, ActualLoc11.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(ActualLoc11));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            machine.Messages.Add(GCommand.CalculateMachineStepsPerMM());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CalculateMachineMMPerPixel()
        {

        }

        public void CalculateMachineStepsPerMM(){
            /*-------------------------------------------------------------------------------------
             - updates, but does not write steps per MM based ActualLoc
             - This is called after Location data is updated
             - Notes:
             -  * fixed distance between monuments - this not changing
             -  * move the steppers to get optically alligned.  If the distance is less than the
             -    fixed distance - too many steps you are over-shooting -> reduce the step rate
             -------------------------------------------------------------------------------------*/
            MachineModel machine = MachineModel.Instance;
                        
            double x_true_dist_mm = (CalTargetModel.GRID_SPACING_X_MILS * Constants.MIL_TO_MM);
            double x_delta_mm = (ActualLoc11.X - ActualLoc00.X);
            Console.WriteLine("X Delta: " + x_true_dist_mm + " - " + x_delta_mm + " ");
            
            double y_true_dist_mm = (CalTargetModel.GRID_SPACING_Y_MILS * Constants.MIL_TO_MM);
            double y_delta_mm = (ActualLoc11.Y - ActualLoc00.Y);
            Console.WriteLine("Y Delta: " + y_true_dist_mm + " - " + y_delta_mm + " ");
                                  
            double xf = (x_delta_mm / x_true_dist_mm);
            double yf = (y_delta_mm / y_true_dist_mm);
            machine.Cal.CalculatedStepsPerUnitX = machine.Cal.StepsPerUnitX * xf;
            machine.Cal.CalculatedStepsPerUnitY = machine.Cal.StepsPerUnitY * yf;

            Console.WriteLine("Actual ----> " + x_true_dist_mm + "," + y_true_dist_mm);
            Console.WriteLine("Delta -----> " + x_delta_mm + "," + y_delta_mm);
            Console.WriteLine("Fraction --> " + xf + " " + machine.Cal.CalculatedStepsPerUnitX + " " + yf + " " + machine.Cal.CalculatedStepsPerUnitY);
            Console.WriteLine("00 " + ActualLoc00.ToString());
            Console.WriteLine("11 " + ActualLoc11.ToString());
        }
    }
}
