using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GraphDigitizer.Screenshot
{
    public static class ScreenshotController
    {
        private static Screenshot _screenshot = null;
        private static Screenshot screenshot 
        { 
            get 
            {
                if (_screenshot == null || _screenshot.IsClosed)
                { 
                    _screenshot = new Screenshot();
                    _screenshot.CloseEvent = new Screenshot.CloseDelegate(Closed);
                }
                return _screenshot; 
            } 
        }

        public delegate void SetImage(BitmapSource bitmapSource);
        public static SetImage SetImageEvent;

        private static void Closed()
        {
            if (screenshot.CapturedBitmap != null)
                SetImageEvent?.Invoke(screenshot.CapturedBitmap);
            screenshot.Close();
        }

        public static void CaptureScreen()
        {
            screenshot.ShowVisually();
        }
    }
}
