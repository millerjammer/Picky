﻿using OpenCvSharp;
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
    public class CalTargetModel :INotifyPropertyChanged
    {
        private MachineModel machine;
        /* Cal Target Model Actual Dimensions */

        /* Calibration Grid Size and Location of 1st */
        public static double TARGET_GRID_X_MILS = 4000;
        public static double TARGET_GRID_Y_MILS = 4000;
        public static double TARGET_GRID_RADIUS_MILS = 50;
        public static double TARGET_GRID_ORIGIN_X_MM = 20.71;
        public static double TARGET_GRID_ORIGIN_Y_MM = 12.89;
        /* Calibration Circle at Tool Z */
        public static double TARGET_TOOL_Z_MILS = 540;
        public static double TARGET_RESOLUTION_RADIUS_MILS = 500;
        public static double TARGET_TOOL_POS_X_MM = 103.00;
        public static double TARGET_TOOL_POS_Y_MM = 65;
        /* Calibration Circle at PCB Z */
        public static double TARGET_PCB_Z_MILS = 70;
        public static double TARGET_PCB_POS_X_MM = 42.01;
        public static double TARGET_PCB_POS_Y_MM = 65.00;

        bool isGrid00Valid, isGrid01Valid, isGrid10Valid, isGrid11Valid;

        /* Actuals */
        private Circle3d grid00Location;
        public Circle3d Grid00Location
        {
            get { return grid00Location; }
            set { grid00Location = value; OnPropertyChanged(nameof(Grid00Location)); }
        }
        private Circle3d grid10Location;
        public Circle3d Grid10Location
        {
            get { return grid10Location; }
            set { grid10Location = value; OnPropertyChanged(nameof(Grid10Location)); }
        }
        private Circle3d grid01Location;
        public Circle3d Grid01Location
        {
            get { return grid01Location; }
            set { grid01Location = value; OnPropertyChanged(nameof(Grid01Location)); }
        }
        private Circle3d grid11Location;
        public Circle3d Grid11Location
        {
            get { return grid11Location; }
            set { grid11Location = value; OnPropertyChanged(nameof(Grid11Location)); }
        }
        private Circle3d calCircle;
        public Circle3d CalCircle
        {
            get { return calCircle; }
            set { calCircle = value; OnPropertyChanged(nameof(CalCircle)); }
        }

        public CalTargetModel()
        {
            Grid00Location = new Circle3d();
            Grid00Location.PropertyChanged += Location_PropertyChanged;
            Grid01Location = new Circle3d();
            Grid01Location.PropertyChanged += Location_PropertyChanged;
            Grid10Location = new Circle3d();
            Grid10Location.PropertyChanged += Location_PropertyChanged;
            Grid11Location = new Circle3d();
            Grid11Location.PropertyChanged += Location_PropertyChanged;

            InvalidateCalibrationTarget();
        }
        
        public void InvalidateCalibrationTarget()
        {
            Grid00Location.IsValid = false;
            Grid01Location.IsValid = false;
            Grid10Location.IsValid = false;
            Grid11Location.IsValid = false;
        }

        public void PerformCalibration(MachineModel mm)
        {
            machine = mm;
            CircleSegment target = new CircleSegment();

            InvalidateCalibrationTarget();
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            target.Radius = (float)(CalTargetModel.TARGET_GRID_RADIUS_MILS * Constants.MIL_TO_MM);
            target.Center.X = (float)CalTargetModel.TARGET_GRID_ORIGIN_X_MM;
            target.Center.Y = (float)CalTargetModel.TARGET_GRID_ORIGIN_Y_MM;
            machine.Messages.Add(GCommand.G_SetPosition(target.Center.X, target.Center.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(target, Grid00Location));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }

            //For the x = 1, y = 0 position
            target.Center = new Point2f((float)(CalTargetModel.TARGET_GRID_ORIGIN_X_MM + (CalTargetModel.TARGET_GRID_X_MILS * Constants.MIL_TO_MM)), (float)(CalTargetModel.TARGET_GRID_ORIGIN_Y_MM));
            machine.Messages.Add(GCommand.G_SetPosition(target.Center.X, target.Center.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(target, Grid10Location));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
                        
            //For the x = 0, y = 1 position
            target.Center = new Point2f((float)(CalTargetModel.TARGET_GRID_ORIGIN_X_MM), (float)(CalTargetModel.TARGET_GRID_ORIGIN_Y_MM + (CalTargetModel.TARGET_GRID_Y_MILS * Constants.MIL_TO_MM)));
            machine.Messages.Add(GCommand.G_SetPosition(target.Center.X, target.Center.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(target, Grid01Location));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }

            //For the x = 1, y = 1 position
            target.Center = new Point2f((float)(CalTargetModel.TARGET_GRID_ORIGIN_X_MM + (CalTargetModel.TARGET_GRID_X_MILS * Constants.MIL_TO_MM)), (float)(CalTargetModel.TARGET_GRID_ORIGIN_Y_MM + (CalTargetModel.TARGET_GRID_Y_MILS * Constants.MIL_TO_MM)));
            machine.Messages.Add(GCommand.G_SetPosition(target.Center.X, target.Center.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(target, Grid11Location));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Location_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((Grid00Location.IsValid == true) && (Grid01Location.IsValid == true) && (Grid10Location.IsValid == true) && (Grid11Location.IsValid == true))
            {
                Console.WriteLine("location 00: " + Grid00Location.ToString());
                Console.WriteLine("location 01: " + Grid01Location.ToString());
                Console.WriteLine("location 10: " + Grid10Location.ToString());
                Console.WriteLine("location 11: " + Grid11Location.ToString());
                CalculateStepsPerMM();
            }
        }

        public void CalculateStepsPerMM()
        {
            double actual_dist_mm = (TARGET_GRID_X_MILS * Constants.MIL_TO_MM);
            double x_delta = (Grid11Location.X - Grid00Location.X);
            double x_err = (x_delta - actual_dist_mm) / actual_dist_mm;
            Console.WriteLine("X Error: " + x_err + "%");

            double y_delta = (Grid11Location.Y - Grid00Location.Y);
            double y_err = (y_delta - actual_dist_mm) / actual_dist_mm;
            Console.WriteLine("Y Error: " + y_err + "%");

            double x_steps_m = (machine.Cal.StepsPerUnitX * x_err);
            double y_steps_m = (machine.Cal.StepsPerUnitY * y_err);

            machine.Cal.CalculatedStepsPerUnitX = x_steps_m + machine.Cal.StepsPerUnitX;
            machine.Cal.CalculatedStepsPerUnitY = y_steps_m + machine.Cal.StepsPerUnitY;

            Console.WriteLine("Proposed change in [X] Steps/mm: " + x_steps_m);
            Console.WriteLine("Proposed change in [Y] Steps/mm: " + y_steps_m);

        }
    }
}
