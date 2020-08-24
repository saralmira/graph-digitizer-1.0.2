using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bitmap = System.Drawing.Bitmap;

namespace GraphDigitizer.Screenshot
{
    /// <summary>
    /// Screenshot.xaml 的交互逻辑
    /// </summary>
    public partial class Screenshot : Window
    {
        public Screenshot()
        {
            InitializeComponent();
            IsCapturing = false;
            IsClosed = false;
        }

        private bool IsCapturing;
        private bool IsDragged;
        private Point StartPoint;
        private Point EndPoint;
        private BitmapSource ScreenBitmap;

        /// <summary>
        /// 标记一下窗体是否已经关闭
        /// </summary>
        public bool IsClosed { get; private set; }
        public BitmapSource CapturedBitmap { get; private set; }

        public delegate void CloseDelegate();
        public CloseDelegate CloseEvent;

        public void ShowVisually()
        {
            //if (IsClosed)
            //    return;
            ScreenBitmap = Util.ScreenSnapshot();
            IsCapturing = false;
            CapturedBitmap = null;
            //var handle = new WindowInteropHelper(this).Handle;
            //Screen screen = Screen.FromHandle(handle);
            this.WindowState = WindowState.Maximized;
            img.Source = ScreenBitmap;
            //this.Visibility = Visibility.Visible;
            StartPoint = new Point();
            EndPoint = new Point(ScreenBitmap.Width, ScreenBitmap.Height);
            this.Show();
        }

        public void CloseVisually()
        {
            this.Visibility = Visibility.Collapsed;
            CloseEvent?.Invoke();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            CloseVisually();
        }

        private void GetCaptured(out double x, out double y, out double width, out double height)
        {
            x = Math.Min(StartPoint.X, EndPoint.X);
            y = Math.Min(StartPoint.Y, EndPoint.Y);
            width = Math.Abs(StartPoint.X - EndPoint.X);
            height = Math.Abs(StartPoint.Y - EndPoint.Y);
        }

        private void Refresh()
        {
            if (IsCapturing)
            {
                GetCaptured(out double x, out double y, out double width, out double height);
                layer_left.Width = x;
                layer_top.Height = y;
                layer_right.Width = this.Width - x - width;
                layer_bottom.Height = this.Height - y - height;
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                CloseVisually();
            }
            else if (e.ChangedButton == MouseButton.Left && !IsCapturing)
            {
                StartPoint = e.GetPosition(this);
                IsCapturing = true;
                IsDragged = false;
                //Refresh();
            }
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsCapturing)
                return;

            IsDragged = true;
            EndPoint = e.GetPosition(this);
            Refresh();
        }

        private void Window_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsCapturing || e.ChangedButton != MouseButton.Left)
                return;
            IsCapturing = false;
            if (!IsDragged)
                CapturedBitmap = ScreenBitmap;
            else if (capture.ActualWidth != 0 && capture.ActualHeight != 0)
            {
                EndPoint = e.GetPosition(this);
                GetCaptured(out double x, out double y, out double width, out double height);
                Util.GetScreenResoCoeff(out double width_coeff, out double height_coeff);
                x /= width_coeff;
                width /= width_coeff;
                y /= height_coeff;
                height /= height_coeff;
                CapturedBitmap = Util.CutImage(ScreenBitmap, new Int32Rect((int)x, (int)y, (int)width, (int)height));
            }
            CloseVisually();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CloseVisually();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
        }
    }
}
