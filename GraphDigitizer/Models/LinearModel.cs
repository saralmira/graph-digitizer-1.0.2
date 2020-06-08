using GraphDigitizer.Views;
using System;
using System.Collections.Generic;
using System.Windows;

namespace GraphDigitizer.Models
{
    public class LinearModel
    {
        public LinearModel(double x1, double y1, double x2, double y2) :
            this(new Vector(x1, y1), new Vector(x2, y2))
        { }

        public LinearModel(Vector p1, Vector p2)
        {
            this.p1 = p1;
            this.p2 = p2;
            Points = new List<Vector>();
        }

        public void Interp(UInt32 count)
        {
            Points.Clear();
            if (count > 0)
            {
                if (count == 1)
                    Points.Add((p1 + p2) / 2);
                else
                {
                    double xgap = (p2.X - p1.X) / (count - 1);
                    double ygap = (p2.Y - p1.Y) / (count - 1);
                    for (int i = 0; i < count; ++i)
                        Points.Add(new Vector(p1.X + xgap * i, p1.Y + ygap * i));
                }
            }
        }

        public void InterpInReal(UInt32 count, bool xaxis = true)
        {
            Points.Clear();
            if (count > 0)
            {
                //this.GetRealCoords(p1, out Coord r1);
                Vector rp1 = Util.ScreenToReal(p1);
                Vector rp2 = Util.ScreenToReal(p2);
                Vector line = p2 - p1;
                if (count == 1)
                {
                    if (xaxis)
                        Points.Add(p1 + line * ((Util.RealToScreen((rp1 + rp2) / 2).X - p1.X) / (p2.X - p1.X)));
                    else
                        Points.Add(p1 + line * ((Util.RealToScreen((rp1 + rp2) / 2).Y - p1.Y) / (p2.Y - p1.Y)));
                }
                else
                {
                    Vector gap = (rp2 - rp1) / (count - 1);
                    for (int i = 0; i < count; ++i)
                    {
                        if (xaxis)
                            Points.Add(p1 + line * ((Util.RealToScreen(rp1 + i * gap).X - p1.X) / (p2.X - p1.X)));
                        else
                            Points.Add(p1 + line * ((Util.RealToScreen(rp1 + i * gap).Y - p1.Y) / (p2.Y - p1.Y)));
                    }
                }
            }
        }

        public readonly List<Vector> Points;

        private Vector p1, p2;
    }
}
