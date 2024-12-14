using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System;
using System.Linq;
using System.Windows.Input;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using TransformGroup = System.Windows.Media.TransformGroup;
using ScaleTransform = System.Windows.Media.ScaleTransform;
using Point = System.Windows.Point;
using Slider = Xamarin.Forms.Slider;
using EnvDTE80;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


/* https://stackoverflow.com/questions/62573753/pan-and-zoom-but-contain-image-inside-parent-container  */
/* https://stackoverflow.com/questions/741956/pan-zoom-image/6782715#6782715 */
/* https://www.kurokesu.com/main/2020/07/12/pulling-full-resolution-from-a-webcam-with-opencv-windows/ */
namespace Picky
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    /// 
        
    public partial class CameraView : UserControl
    {
        CameraViewModel camera;

        System.Windows.Media.TransformGroup group;
        System.Windows.Media.ScaleTransform xform;
        System.Windows.Media.TranslateTransform tt;

        private Point start, origin;
        
        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public CameraView(CameraModel cam)
        {
            InitializeComponent();
            camera = new CameraViewModel(FrameImage, cam);
            this.DataContext = camera;
            
            group = new System.Windows.Media.TransformGroup();
            xform = new System.Windows.Media.ScaleTransform();
            xform.ScaleX = 1;
            xform.ScaleY = 1;
            group.Children.Add(xform);
            tt = new System.Windows.Media.TranslateTransform();
            group.Children.Add(tt);

            FrameImage.RenderTransform = group;
            FrameImage.RenderTransformOrigin = new Point(0, 0);
                        
            FrameImage.MouseWheel += Image_MouseWheel;
            FrameImage.MouseLeftButtonDown += Image_MouseDown;
            FrameImage.MouseLeftButtonUp += Image_MouseUp;
            FrameImage.MouseMove += Image_MouseMove;
            PreviewMouseRightButtonDown += delegate { Reset(); };
          
        }
   

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameImage.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (FrameImage.IsMouseCaptured)
            {
                var st = GetScaleTransform(FrameImage);
                var tt = GetTranslateTransform(FrameImage);
                Vector v = start - e.GetPosition(this);
               
                /* Keep origin within frame */
                if ((origin.X - v.X) < 0)
                    tt.X = (origin.X - v.X);
                else
                    tt.X = 0;
                if((origin.Y - v.Y) < 0)
                    tt.Y = (origin.Y - v.Y);
                else
                    tt.Y = 0;
                /* Keep extents within frame */
                if ( ( -(origin.X - v.X) + FrameImage.ActualWidth) > (FrameImage.ActualWidth * st.ScaleX))
                    tt.X = FrameImage.ActualWidth - (FrameImage.ActualWidth * st.ScaleX);
                if ((-(origin.Y - v.Y) + FrameImage.ActualHeight) > (FrameImage.ActualHeight * st.ScaleY))
                    tt.Y = FrameImage.ActualHeight - (FrameImage.ActualHeight * st.ScaleY);
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var tt = GetTranslateTransform(FrameImage);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            this.Cursor = Cursors.Hand;
            FrameImage.CaptureMouse();
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var st = GetScaleTransform(FrameImage);
            var tt = GetTranslateTransform(FrameImage);

            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                return;

            Point relative = e.GetPosition(FrameImage);
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            if(st.ScaleX < 1)
                st.ScaleX = 1;
            if (st.ScaleY < 1)
                st.ScaleY = 1;
                      
            /* Keep origin within frame */
            if ((absoluteX - relative.X * st.ScaleX) < 0)
                tt.X = (absoluteX - relative.X * st.ScaleX);
            else
                tt.X = 0;
            if ((absoluteY - relative.Y * st.ScaleY) < 0)
                tt.Y = (absoluteY - relative.Y * st.ScaleY);
            else
                tt.Y = 0;
            /* Keep extents within frame */
            if ((-(absoluteX - relative.X * st.ScaleX) + FrameImage.ActualWidth) > (FrameImage.ActualWidth * st.ScaleX))
                tt.X = FrameImage.ActualWidth - (FrameImage.ActualWidth * st.ScaleX);
            if ((-(absoluteY - relative.Y * st.ScaleY) + FrameImage.ActualHeight) > (FrameImage.ActualHeight * st.ScaleY))
                tt.Y = FrameImage.ActualHeight - (FrameImage.ActualHeight * st.ScaleY);
        }

        private void Reset()
        {
            var st = GetScaleTransform(FrameImage);
            xform.ScaleX = 1.0;
            xform.ScaleY = 1.0;

            var tt = GetTranslateTransform(FrameImage);
            tt.X = 0.0;
            tt.Y = 0.0;
        }
             
        public void FocusDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.camera.Settings.Focus = (int)((Slider)sender).Value;
        }

        public void ThresholdDragCompleted(object sender, DragCompletedEventArgs e)
        {
            camera.camera.Settings.TemplateThreshold = (int)((Slider)sender).Value;
        }
            
    }
}
