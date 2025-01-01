using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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

        private Position3D Grid00, Grid11;

        /* Upper and Lower Resolution Targets */
        [JsonIgnore]
        private Mat upperTemplate;
        [JsonIgnore]
        private Mat lowerTemplate;
        [JsonIgnore]
        private Mat gridTemplate;

        private Position3D actualLocUpper;
        public Position3D ActualLocUpper
        {
            get { return actualLocUpper;  }
            set { actualLocUpper = value; OnPropertyChanged(nameof(ActualLocUpper)); }
        }

        private Position3D actualLocLower;
        public Position3D ActualLocLower
        {
            get { return actualLocLower; }
            set { actualLocLower = value; OnPropertyChanged(nameof(ActualLocLower)); }
        }

        private Position3D grid;
        public Position3D Grid
        {
            get { return grid; }
            set { grid = value; OnPropertyChanged(nameof(Grid)); }
        }
       
        private string upperTemplateFileName;
        public string UpperTemplateFileName
        {
            get { return upperTemplateFileName; }
            set { upperTemplateFileName = value; OnPropertyChanged(nameof(UpperTemplateFileName)); }
        }

        private string lowerTemplateFileName;
        public string LowerTemplateFileName
        {
            get { return lowerTemplateFileName; }
            set { lowerTemplateFileName = value; OnPropertyChanged(nameof(LowerTemplateFileName)); }
        }

        private string gridTemplateFileName;
        public string GridTemplateFileName
        {
            get { return gridTemplateFileName; }
            set { gridTemplateFileName = value; OnPropertyChanged(nameof(GridTemplateFileName)); }
        }

        public CameraSettings upperSettings { get; set; }   
        public CameraSettings lowerSettings { get; set; }
        public CameraSettings gridSettings { get; set; }

        public CalTargetModel()
        {
            Grid = new Position3D();
            Grid.Width = 100;
            Grid.Height = 100;
        }

        public ICommand SetUpperCalTargetCommand { get { return new RelayCommand(SetUpperCalTarget); } }
        public void SetUpperCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            ActualLocUpper = new Position3D(machine.CurrentX, machine.CurrentY);
            upperSettings = machine.downCamera.Settings.Clone();
            UpperTemplateFileName = SetCalTarget(machine, upperTemplate);
        }

        public ICommand SetLowerCalTargetCommand { get { return new RelayCommand(SetLowerCalTarget); } }
        public void SetLowerCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            ActualLocLower = new Position3D(machine.CurrentX, machine.CurrentY);
            lowerSettings = machine.downCamera.Settings.Clone();
            LowerTemplateFileName = SetCalTarget(machine, lowerTemplate);
        }

        public ICommand SetGridCalTargetCommand { get { return new RelayCommand(SetGridCalTarget); } }
        public void SetGridCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            Grid.X = machine.CurrentX; Grid.Y = machine.CurrentY;
            gridSettings = machine.downCamera.Settings.Clone();
            GridTemplateFileName = SetCalTarget(machine, gridTemplate);
        }
                       
        private string SetCalTarget(MachineModel machine, Mat template) { 
        /*---------------------------------------------------------------
         * Private routine for saving calibration target template and grid
         * Don't call here directly. ROI is centered in frame
         * -------------------------------------------------------------*/
                             
            int width = (Constants.CAMERA_FRAME_WIDTH / 6);
            int height = (Constants.CAMERA_FRAME_HEIGHT / 6);
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);

            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, width, height);
            template = new Mat(machine.downCamera.ColorImage, roi);
                 
            // Write part template to file
            DateTime now = DateTime.Now;
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String filename = path + "\\calTemplate-" + now.ToString("MMddHHmmss") + ".png";
            Cv2.ImWrite(filename, template);
            while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { } 
            
            Console.WriteLine("Set Cal Target Template: " + filename);
            return filename;
        }

        public void CalibrateMMPerPixelAtZ()
        /*---------------------------------------------------------------------
         * Called by GUI for calibration
         * 
         * Uses step alignment to determine mm/pix at a specific target. This
         * is a calibration used to enable jump step connections based on cammea
         * to target.  This function is performed at two different target z 
         * elevations.  This should be done after calibrating mm per steps.
         * Requires that Z is calibrated at the calibration pad
         * -------------------------------------------------------------------*/
        {
            MachineModel machine = MachineModel.Instance;
            
            /* Get Z distance for each */
            double dist_to_deck_mm = machine.Cal.CalPad.Z + Constants.ZOFFSET_CAL_PAD_TO_DECK;
            machine.Cal.MMPerPixLower.Z = dist_to_deck_mm - LOWER_GRID_Z_OFFSET_FROM_DECK_MM;
            machine.Cal.MMPerPixUpper.Z = dist_to_deck_mm - UPPER_LOWER_GRID_Z_OFFSET_MM;
            
            /* Get Upper */
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            QueueCalTargetSearch(upperTemplateFileName, ActualLocUpper, upperSettings, machine.Cal.MMPerPixUpper, 5.5);
            QueueCalTargetSearch(lowerTemplateFileName, ActualLocLower, lowerSettings, machine.Cal.MMPerPixLower, 5);
        }

        public void QueueCalTargetSearch(string templateFile, Position3D pos, CameraSettings settings, Position3D res, double roi_factor)
        {
            /*--------------------------------------------------------------
             * Called by GUI for calibration or preview.  Establishes a
             * search roi to get 8 templated from template search.  
             * For calibration set preview buttons false. roi_factor is the
             * search area factor based on dimensions of the template
             * ------------------------------------------------------------*/
            
            MachineModel machine = MachineModel.Instance;
            Mat template = Cv2.ImRead(templateFile, ImreadModes.Color);

            /* Search Area */
            int width = (int)(roi_factor * template.Width);
            int height = (int)(roi_factor * template.Height);
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);

            OpenCvSharp.Rect search_roi = new OpenCvSharp.Rect(x, y, width, height);

            machine.Messages.Add(GCommand.G_SetPosition(pos.X, pos.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.SetCamera(settings, machine.downCamera));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.GetGridCalibration(template, search_roi, res));

        }
        
        public void CalibrateMMPerStep()
        /*---------------------------------------------------------------------
         * Called by GUI
         * 
         * Uses step alignment to determine mm/step. This
         * is a calibration used to verify or update settings in the stepper 
         * motor controller. This should be the first calibration that's done.
         *  - TODO Use a calibrated Z.  This will read current steps per mm.
         * -------------------------------------------------------------------*/
        {
            MachineModel machine = MachineModel.Instance;

            // TODO Replace with calibrated Z
            Grid00 = new Position3D(Grid.X, Grid.Y);
            Grid00.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            Grid00.Radius = GRID_MONUMENT_RADIUS_MM;

            Grid11 = new Position3D(Grid.X + Grid.Width, Grid.Y + Grid.Height);
            Grid11.Z = (Constants.CAMERA_TO_DECK_MILS * Constants.MIL_TO_MM) - GRID_MONUMENT_HEIGHT_MM;
            Grid11.Radius = GRID_MONUMENT_RADIUS_MM;
            
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            // For the x = 0, y = 0 position
            machine.Messages.Add(GCommand.G_SetPosition(Grid.X, Grid.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(Grid00));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            //For the x = 1, y = 1 position
            machine.Messages.Add(GCommand.G_SetPosition(Grid.X + Grid.Width, Grid.Y + Grid.Height, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.StepAlignToCalCircle(Grid11));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            machine.Messages.Add(GCommand.CalculateMachineStepsPerMM());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
              

        public void CalculateMachineStepsPerMM(){
            /*-------------------------------------------------------------------------------------
             - Called by Command Queue
             - updates, but does not write steps per MM based ActualLoc
             - This is called after Location data is updated
             - Notes:
             -  * fixed distance between monuments - this not changing
             -  * move the steppers to get optically alligned.  If the distance is less than the
             -    fixed distance - too many steps you are over-shooting -> reduce the step rate
             -------------------------------------------------------------------------------------*/
            MachineModel machine = MachineModel.Instance;
                        
            double x_true_dist_mm = (CalTargetModel.GRID_SPACING_X_MILS * Constants.MIL_TO_MM);
            double x_delta_mm = (Grid11.X - Grid00.X);
            Console.WriteLine("X Delta: " + x_true_dist_mm + " - " + x_delta_mm + " ");
            
            double y_true_dist_mm = (CalTargetModel.GRID_SPACING_Y_MILS * Constants.MIL_TO_MM);
            double y_delta_mm = (Grid11.Y - Grid00.Y);
            Console.WriteLine("Y Delta: " + y_true_dist_mm + " - " + y_delta_mm + " ");
                                  
            double xf = (x_delta_mm / x_true_dist_mm);
            double yf = (y_delta_mm / y_true_dist_mm);
            machine.Cal.CalculatedStepsPerUnitX = machine.Cal.StepsPerUnitX * xf;
            machine.Cal.CalculatedStepsPerUnitY = machine.Cal.StepsPerUnitY * yf;

            Console.WriteLine("Actual ----> " + x_true_dist_mm + "," + y_true_dist_mm);
            Console.WriteLine("Delta -----> " + x_delta_mm + "," + y_delta_mm);
            Console.WriteLine("Fraction --> " + xf + " " + machine.Cal.CalculatedStepsPerUnitX + " " + yf + " " + machine.Cal.CalculatedStepsPerUnitY);
            Console.WriteLine("00 " + Grid00.ToString());
            Console.WriteLine("11 " + Grid11.ToString());
        }
    }
}
