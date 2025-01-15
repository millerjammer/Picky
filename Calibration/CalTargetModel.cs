using EnvDTE90;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using Picky.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using static Picky.MachineMessage;

namespace Picky
{
    public class CalTargetModel : INotifyPropertyChanged
    {
        /* Physical properties of the Target */
        public static double GRID_DEFAULT_X_MM = 24.3;
        public static double GRID_DEFAULT_Y_MM = 23.8;
        public static double GRID_SPACING_X_MM = 100;
        public static double GRID_SPACING_Y_MM = 100;
                
        public static double LOWER_GRID_Z_OFFSET_FROM_DECK_MM = 1;
        public static double UPPER_LOWER_GRID_Z_OFFSET_MM = 10;

        public static double OPTICAL_GRID_X_MM = 10;
        public static double OPTICAL_GRID_Y_MM = 10;
        public static double OPTICAL_GRID_RADIUS_MM = 1.5;

        public static double STEP_DEFAULT_X_MM = 122;
        public static double STEP_DEFAULT_Y_MM = 10;
        public static double STEP_SPACING_X_MM = 20;
        public static double STEP_SPACING_Y_MM = 0;
        public static double STEP_SPACING_Z_MM = 5;
        public static double STEP_COUNT = 5;


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

        private Position3D gridOrigin = new Position3D(GRID_DEFAULT_X_MM, GRID_DEFAULT_Y_MM, 0, GRID_SPACING_X_MM, GRID_SPACING_Y_MM);
        public Position3D GridOrigin
        {
            get { return gridOrigin; }
            set { gridOrigin = value; OnPropertyChanged(nameof(GridOrigin)); }
        }

        private Position3D stepPad = new Position3D(STEP_DEFAULT_X_MM, STEP_DEFAULT_Y_MM);
        public Position3D StepPad
        {
            get { return stepPad; }
            set { stepPad = value; OnPropertyChanged(nameof(StepPad)); }
        }

        private Position3D grid00 { get; set; }
        private Position3D grid11 { get; set; }
        // Create list of positions - inline
        private List<Position3D> zStepActual = Enumerable.Range(0, 10).Select(_ => new Position3D()).ToList();

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
        }

        public ICommand SetUpperCalTargetCommand { get { return new RelayCommand(SetUpperCalTarget); } }
        public void SetUpperCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            ActualLocUpper = new Position3D(machine.Current.X, machine.Current.Y);
            upperSettings = machine.downCamera.Settings.Clone();
            UpperTemplateFileName = SetCalTarget(machine, upperTemplate);
        }

        public ICommand SetLowerCalTargetCommand { get { return new RelayCommand(SetLowerCalTarget); } }
        public void SetLowerCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            ActualLocLower = new Position3D(machine.Current.X, machine.Current.Y);
            lowerSettings = machine.downCamera.Settings.Clone();
            LowerTemplateFileName = SetCalTarget(machine, lowerTemplate);
        }

        public ICommand SetGridCalTargetCommand { get { return new RelayCommand(SetGridCalTarget); } }
        public void SetGridCalTarget()
        {
            MachineModel machine = MachineModel.Instance;
            GridOrigin.X = machine.Current.X; GridOrigin.Y = machine.Current.Y;
            gridSettings = machine.downCamera.Settings.Clone();
            GridTemplateFileName = SetCalTarget(machine, gridTemplate);
        }
                       
        private string SetCalTarget(MachineModel machine, Mat template) { 
        /*---------------------------------------------------------------
         * Private routine for saving calibration template template and gridOrigin
         * Don't call here directly. ROI is centered in frame
         * -------------------------------------------------------------*/
                             
            int width = (Constants.CAMERA_FRAME_WIDTH / 6);
            int height = (Constants.CAMERA_FRAME_HEIGHT / 6);
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);

            OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x, y, width, height);
            template = new Mat(machine.downCamera.ColorImage, roi);
                 
            // Write Part template to file
            DateTime now = DateTime.Now;
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String filename = path + "\\calTemplate-" + now.ToString("MMddHHmmss") + ".png";
            Cv2.ImWrite(filename, template);
            while (File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read) == null) { } 
            
            Console.WriteLine("Set Cal Target Template: " + filename);
            return filename;
        }

        public void TestMMPerPixelAtZ()
        /*---------------------------------------------------------------------
         * Called by GUI to test Calibration
         * -------------------------------------------------------------------*/
        {
            double roi_factor = 6;
            MachineModel machine = MachineModel.Instance;

            /* Upper Search Area */
            Mat upperTemplate = Cv2.ImRead(UpperTemplateFileName, ImreadModes.Color);
            int width = (int)(roi_factor * upperTemplate.Width);
            int height = (int)(roi_factor * upperTemplate.Height);
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);
            Position3D roi_upper = new Position3D(x, y, machine.Cal.MMPerPixUpper.Z, width, height);

            /* Lower Search Area */
            Mat lowerTemplate = Cv2.ImRead(LowerTemplateFileName, ImreadModes.Color);
            width = (int)(roi_factor * lowerTemplate.Width);
            height = (int)(roi_factor * lowerTemplate.Height);
            x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);
            Position3D roi_lower = new Position3D(x, y, machine.Cal.MMPerPixLower.Z, width, height);

            /* Upper */
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.SetCamera(upperSettings, machine.downCamera));
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocUpper.X, ActualLocUpper.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.Delay(2000));
            machine.Messages.Add(GCommand.GetTemplatePosition(upperTemplate, roi_upper));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));

            /* Lower */
            machine.Messages.Add(GCommand.SetCamera(lowerSettings, machine.downCamera));
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocLower.X, ActualLocLower.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.Delay(2000));
            machine.Messages.Add(GCommand.GetTemplatePosition(lowerTemplate, roi_lower));
            machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
        }


        public void CalibrateMMPerPixelAtZ()
        /*---------------------------------------------------------------------
         * Called by GUI for calibration
         * 
         * Uses step alignment to determine mm/pix at a specific template. This
         * is a calibration used to enable jump step connections based on cammea
         * to template.  This function is performed at two different template z 
         * elevations.  This should be done after calibrating mm per steps.
         * Requires that Z is calibrated at the calibration pad
         * -------------------------------------------------------------------*/
        {
            MachineModel machine = MachineModel.Instance;
            
            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            /* Probe for upper Z */
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocUpper.X, ActualLocUpper.Y, 0, 0, 0));
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_EndstopStates());
            machine.Messages.Add(GCommand.GetZProbe(machine.Cal.MMPerPixUpper));
            /* Queue for upper */
            QueueCalTargetSearch(upperTemplateFileName, ActualLocUpper, upperSettings, machine.Cal.MMPerPixUpper, 5.5);
            
            machine.Messages.Add(GCommand.G_SetPosition(ActualLocLower.X, ActualLocLower.Y, 0, 0, 0));
            /* Probe for lower Z */
            machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
            machine.Messages.Add(GCommand.G_FinishMoves());
            machine.Messages.Add(GCommand.G_EndstopStates());
            machine.Messages.Add(GCommand.GetZProbe(machine.Cal.MMPerPixLower));
            /* Queue for lower */
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
            machine.Messages.Add(GCommand.Get3x3GridCalibration(template, search_roi, res));

        }
        
        public void CalibrateMMPerStep()
        /*---------------------------------------------------------------------
         * Called by GUI
         * 
         * Uses step alignment to determine mm/step. This
         * is a calibration used to verify or update settings in the stepper 
         * motor controller. This should be the first calibration that's done.
         * 
         * -------------------------------------------------------------------*/
        {
            double roi_factor = 3;
            double y_offset;
            MachineModel machine = MachineModel.Instance;
            Mat template = Cv2.ImRead(GridTemplateFileName, ImreadModes.Color);

            /* Search Area */
            int width = (int)(roi_factor * template.Width);
            int height = (int)(roi_factor * template.Height);
            int x = (Constants.CAMERA_FRAME_WIDTH / 2) - (width / 2);
            int y = (Constants.CAMERA_FRAME_HEIGHT / 2) - (height / 2);

            OpenCvSharp.Rect search_roi = new OpenCvSharp.Rect(x, y, width, height);
            grid00 = new Position3D(); grid11 = new Position3D();

            machine.Messages.Add(GCommand.G_EnableIlluminator(true));
            machine.Messages.Add(GCommand.G_GetStepsPerUnit());
            // Start with Z Calibration
            for(int i = 0;i < STEP_COUNT; i++)
            {
                y_offset = machine.Cal.Target.StepPad.Y + (i * STEP_SPACING_Y_MM) + Constants.CAMERA_TO_HEAD_OFFSET_Y_MM;
                machine.Messages.Add(GCommand.G_SetPosition(machine.Cal.Target.StepPad.X - (i * STEP_SPACING_X_MM), (int)y_offset, 0, 0, 0));
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.G_ProbeZ(Constants.ZPROBE_LIMIT));
                machine.Messages.Add(GCommand.Delay(500));
                machine.Messages.Add(GCommand.GetZProbe(zStepActual.ElementAt(i)));
                machine.Messages.Add(GCommand.G_SetZPosition(0));
                machine.Messages.Add(GCommand.G_FinishMoves());
            }
            machine.Messages.Add(GCommand.SetCamera(gridSettings, machine.downCamera));
            machine.Messages.Add(GCommand.G_SetAbsolutePositioningMode(true));
            // For the x = 0, y = 0 position
            machine.Messages.Add(GCommand.G_SetPosition(GridOrigin.X, GridOrigin.Y, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.Delay(500));
                machine.Messages.Add(GCommand.StepAlignToTemplate(template, search_roi, grid00));
                machine.Messages.Add(GCommand.G_SetPosition(0, 0, 0, 0, 0));
            }
            //For the x = 1, y = 1 position
            machine.Messages.Add(GCommand.G_SetPosition(GridOrigin.X + GridOrigin.Width, GridOrigin.Y + GridOrigin.Height, 0, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                machine.Messages.Add(GCommand.G_FinishMoves());
                machine.Messages.Add(GCommand.Delay(500));
                machine.Messages.Add(GCommand.StepAlignToTemplate(template, search_roi, grid11));
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
                        
            double x_true_dist_mm = CalTargetModel.GRID_SPACING_X_MM;
            double x_delta_mm = (grid11.X - grid00.X);
            Console.WriteLine("X Delta: " + x_true_dist_mm + " - " + x_delta_mm + " ");
            
            double y_true_dist_mm = CalTargetModel.GRID_SPACING_Y_MM;
            double y_delta_mm = (grid11.Y - grid00.Y);
            Console.WriteLine("Y Delta: " + y_true_dist_mm + " - " + y_delta_mm + " ");

            double z_true_dist_mm = (3 * CalTargetModel.STEP_SPACING_Z_MM);
            double z_delta_mm = (zStepActual.ElementAt(3).Z - zStepActual.ElementAt(0).Z);
            Console.WriteLine("Z Delta: " + z_true_dist_mm + " - " + z_delta_mm + " ");

            double xf = (x_delta_mm / x_true_dist_mm);
            double yf = (y_delta_mm / y_true_dist_mm);
            double zf = (z_delta_mm / z_true_dist_mm);
            machine.Cal.CalculatedStepsPerUnitX = machine.Cal.StepsPerUnitX * xf;
            machine.Cal.CalculatedStepsPerUnitY = machine.Cal.StepsPerUnitY * yf;
            machine.Cal.CalculatedStepsPerUnitZ = machine.Cal.StepsPerUnitZ * zf;

            Console.WriteLine("Actual ----> " + x_true_dist_mm + "," + y_true_dist_mm + "," + z_true_dist_mm);
            Console.WriteLine("Delta -----> " + x_delta_mm + "," + y_delta_mm + "," + z_delta_mm);
            Console.WriteLine("Fraction --> " + xf + " " + machine.Cal.CalculatedStepsPerUnitX + " " + yf + " " + machine.Cal.CalculatedStepsPerUnitY + zf + " " + machine.Cal.CalculatedStepsPerUnitZ);
            Console.WriteLine("00 " + grid00.ToString());
            Console.WriteLine("11 " + grid11.ToString());
        }
    }
}
