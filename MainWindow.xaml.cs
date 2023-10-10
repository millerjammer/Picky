using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Windows.Data;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using static Picky.MachineMessage;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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
        private readonly VideoCapture capture;
        private readonly BackgroundWorker bkgWorker;
                      
        private List<Part> pickList;
        
        public MainWindow()
        {
            InitializeComponent();

            capture = new VideoCapture();
            
            bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            bkgWorker.DoWork += Worker_DoWork;

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

        }
        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            //capture.Set(VideoCaptureProperties.FrameWidth, 1920);
            //capture.Set(VideoCaptureProperties.FrameHeight, 1080);
            // 5Mpix Settings
            capture.Set(VideoCaptureProperties.FrameWidth, 2592);
            capture.Set(VideoCaptureProperties.FrameHeight, 1944);

            capture.Open(0);
            if (!capture.IsOpened())
            {
                Close();
                return;
            }
            //double maxHeight = capture.Get(VideoCaptureProperties.FrameHeight);
            //double maxWidth = capture.Get(VideoCaptureProperties.FrameWidth);
            //Console.Write("here: " + maxHeight + " " +maxWidth + "\n");

            bkgWorker.RunWorkerAsync();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bkgWorker.CancelAsync();
            capture.Dispose();
            
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }
                Thread.Sleep(30);
            }
        }

        /*

        void OnButtonStop(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_EnableSteppers((byte)(Constants.A_AXIS | Constants.B_AXIS | Constants.X_AXIS | Constants.Y_AXIS | Constants.Z_AXIS), false));
        }

        void OnHome(object sender, EventArgs e)
        {
            machine.messages.Enqueue(MachineCommands.S3G_Initialize());
            machine.messages.Enqueue(MachineCommands.S3G_FindXYMaximums());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
            machine.messages.Enqueue(MachineCommands.S3G_FindZMinimum());
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
            machine.messages.Enqueue(MachineCommands.S3G_SetPositionAs(0, 0, 0, 0, 0));
            machine.messages.Enqueue(MachineCommands.S3G_SetAbsoluteAngle(0));
            machine.messages.Enqueue(MachineCommands.S3G_SetAbsoluteXYPosition(-50, -50));
            machine.messages.Enqueue(MachineCommands.S3G_GetPosition());
        }*/

        void OnLoadPickFile(object sender, EventArgs e)
        {
            bool isBody = false; int last, len, first = 0;
            int[] startIndex = new int[8];
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Pick And Place Files (*.pnp, *.txt)|*.pnp;*.txt";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                pickList = new List<Part>();
                var lines = File.ReadAllLines(openFileDialog.FileName);
                for(int i = 0; i < lines.Length; i++)
                {                
                    if (!isBody)
                    {
                        if (lines[i].IndexOf("Designator") == 0)
                        {
                            startIndex[0] = 0;
                            startIndex[1] = lines[i].IndexOf("Comment");
                            startIndex[2] = lines[i].IndexOf("Layer");
                            startIndex[3] = lines[i].IndexOf("Footprint");
                            startIndex[4] = lines[i].IndexOf("Center-X");
                            startIndex[5] = lines[i].IndexOf("Center-Y");
                            startIndex[6] = lines[i].IndexOf("Rotation");
                            startIndex[7] = lines[i].IndexOf("Description");
                            isBody = true;
                        }
                    }
                    else
                    {
                        Part part = new Part();
                        part.Designator = lines[i].Substring(startIndex[0], lines[i].IndexOf(' ', startIndex[0]) - startIndex[0]);
                        if (lines[i].IndexOf('\"', startIndex[1], 1) >= 0)
                            part.Comment = lines[i].Substring(startIndex[1] + 1, lines[i].IndexOf('\"', startIndex[1] + 1) - startIndex[1] - 1);
                        else
                            part.Comment = lines[i].Substring(startIndex[1], lines[i].IndexOf(' ', startIndex[1]) - startIndex[1]);
                        part.Layer = lines[i].Substring(startIndex[2], lines[i].IndexOf(' ', startIndex[2]) - startIndex[2]);
                        part.Footprint = lines[i].Substring(startIndex[3], lines[i].IndexOf(' ', startIndex[3]) - startIndex[3]);
                        part.CenterX = lines[i].Substring(startIndex[4], lines[i].IndexOf(' ', startIndex[4]) - startIndex[4]);
                        part.CenterY = lines[i].Substring(startIndex[5], lines[i].IndexOf(' ', startIndex[5]) - startIndex[5]);
                        part.Rotation = lines[i].Substring(startIndex[6], lines[i].IndexOf(' ', startIndex[6]) - startIndex[6]);
                        part.Description = lines[i].Substring(startIndex[7] + 1, lines[i].IndexOf('\"', startIndex[7] + 1) - startIndex[7] - 1);

                        Console.WriteLine("Part: " + part.Description);
                        pickList.Add(part);
                    }
                }
                pickListView.ItemsSource = pickList;
                return;
            }
        }
    }
}
