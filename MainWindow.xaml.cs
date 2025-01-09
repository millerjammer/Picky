using System.ComponentModel;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using static Picky.MachineMessage;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

/********************************************************************
 * Notes:
 * 
 * For GUI See https://github.com/lepoco/wpfui
 * For S3G Serial Commands See: https://github.com/makerbot/s3g/blob/master/doc/s3gProtocol.md
 * 
 *********************************************************************/

namespace Picky
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
               
        private readonly CassetteView       cassetteView;
        public MachineModel machine { get; set; }
        public SerialInterface serialInterface { get; set; }
        private readonly PickListView       pickListView;
        private readonly PickToolWindow     toolWindow;
        private readonly ControlWindow      controlWindow;
        private readonly SettingsWindow     settingsWindow;
        private readonly FeederWindow       feederWindow;

        public MainWindow()
        {

            InitializeComponent();
            this.Closing += MainWindow_Closing;
            machine = MachineModel.Instance;
            serialInterface = new SerialInterface();

            cassetteView = new CassetteView();
            cView.Children.Add(cassetteView);

            pickListView = new PickListView();
            lView.Children.Add(pickListView);

            toolWindow = new PickToolWindow();
            settingsWindow = new SettingsWindow();
            feederWindow = new FeederWindow();
                        
            controlWindow = new ControlWindow();
            DataContext = this;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Terminate the application
            machine.upCamera.Dispose();
            machine.downCamera.Dispose();
            serialInterface.Dispose();
            Application.Current.Shutdown();
        }

        public ICommand OnToolsCommand { get { return new RelayCommand(onTools); } }
        private void onTools()
        {
            toolWindow.Show();
            toolWindow.Activate();
        }

        public ICommand OnFeedersCommand { get { return new RelayCommand(onFeeders); } }
        private void onFeeders()
        {
            feederWindow.Show();
            feederWindow.Activate();
        }

        public ICommand OnControlsCommand { get { return new RelayCommand(onControl); } }
        private void onControl()
        {
            controlWindow.Show();
            controlWindow.Activate();
        }

        public ICommand OnCalibrateCommand { get { return new RelayCommand(onCalibrate); } }
        private void onCalibrate()
        {
            controlWindow.calibrationWindow.Show();
            controlWindow.calibrationWindow.Activate();
        }

        public ICommand OnSettingsCommand { get { return new RelayCommand(onSettings); } }
        private void onSettings()
        {
            settingsWindow.Show();
            settingsWindow.Activate();
        }

        public ICommand OnAssembleCommand { get { return new RelayCommand(onAssemble); } }
        private void onAssemble()
        {
            controlWindow.messageWindow.Show();
            controlWindow.messageWindow.Activate();
        }

    }
}
