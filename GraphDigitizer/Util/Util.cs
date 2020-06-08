using GraphDigitizer.Views;
using System.Windows;

namespace GraphDigitizer
{
    public static class Util
    {
        public static Vector ScreenToReal(Vector p)
        {
            return (Vector)MainWindow.ScreenToReal?.Invoke(p);
        }

        public static Vector RealToScreen(Vector p)
        {
            return (Vector)MainWindow.RealToScreen?.Invoke(p);
        }
    }
}
