using GraphDigitizer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
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

        public static void AddScreenPoint(double x, double y)
        {
            MainWindow.AddScreenPoint?.Invoke(x, y);
        }

        public static void AddRealPoint(double realx, double realy)
        {
            MainWindow.AddRealPoint?.Invoke(realx, realy);
        }

        public static string Encode(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();
        }

        public static byte[] Decode(string str)
        {
            if (str.Length > 0 && str.Length % 2 == 0)
            {
                var arr = new byte[str.Length / 2];
                for (int i = 0; i < arr.Length; ++i)
                    arr[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                return arr;
            }
            return null;
        }
    }

    public abstract class NotifyClass : INotifyPropertyChanged
    {
        public class NotifyList<T> : ObservableCollection<T> 
        {
            public NotifyList() : base() { }
            public NotifyList(List<T> list) : base(list) { }
            public NotifyList(IEnumerable<T> collection) : base(collection) { }

            public void AddRange(ICollection<T> list)
            {
                foreach (T t in list)
                    this.Add(t);
            }
        }

        protected void Notify(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
