using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphDigitizer.Views;

namespace GraphDigitizer.Models
{
    public class Axes
    {
        public Coord Xmin, Xmax, Ymin, Ymax;
        public int Status; //Which is the next point to assign, in the order above
        public bool XLog, YLog; //If the axes are logarithmic or not
        public Line Xaxis, Yaxis;
    }

    public class Crosshair
    {
        readonly Brush red = new SolidColorBrush(Color.FromArgb(0x90, 0xFF, 0x21, 0x21));
        readonly Brush blue = new SolidColorBrush(Color.FromArgb(0x90, 0x21, 0x21, 0xFF));
        readonly Brush green = new SolidColorBrush(Color.FromArgb(0x90, 0x21, 0xFF, 0x21));
        readonly Brush point = new SolidColorBrush(Color.FromArgb(0x90, 0xFC, 0x8A, 0x45));

        public Crosshair()
        {
            X = new Line() 
            { 
                Stroke = red,
                Focusable = false,
                IsHitTestVisible = false, 
                StrokeThickness = 2
            };
            Y = new Line() 
            { 
                Stroke = red,
                Focusable = false,
                IsHitTestVisible = false, 
                StrokeThickness = 2 
            };
            Hide(true);
        }

        public void SetHorizental(double x1, double y1, double x2, double y2)
        {
            X.X1 = x1;
            X.X2 = x2;
            X.Y1 = y1;
            X.Y2 = y2;
        }

        public void SetVertical(double x1, double y1, double x2, double y2)
        {
            Y.X1 = x1;
            Y.X2 = x2;
            Y.Y1 = y1;
            Y.Y2 = y2;
        }

        public void Hide(bool hide)
        {
            if (ishide == hide)
                return;
            X.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            Y.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            ishide = hide;
        }

        public void SetState(State state, int status = 0)
        {
            if (state == State.Line)
            {
                X.Stroke = green;
                Y.Stroke = green;
            }
            else if (state == State.Points)
            {
                X.Stroke = point;
                Y.Stroke = point;
            }
            else
            {
                switch (status)
                {
                    case 2:
                    case 3:
                        X.Stroke = blue;
                        Y.Stroke = blue;
                        break;
                    default:
                        X.Stroke = red;
                        Y.Stroke = red;
                        break;
                }
            }
        }

        private bool ishide = false;
        public Line X, Y;
    }
}
