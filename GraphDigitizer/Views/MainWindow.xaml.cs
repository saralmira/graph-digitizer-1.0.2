using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphDigitizer.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out XYPoint p);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ClipCursor")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ClipCursor(ref Rect r);

        public delegate Vector delCoordTransform(Vector p);
        public static delCoordTransform ScreenToReal;
        public static delCoordTransform RealToScreen;

        public delegate void delAddPoint(double realx, double realy);
        public static delAddPoint AddScreenPoint;
        public static delAddPoint AddRealPoint;
        public delegate void delAddPoints();
        public static delAddPoints AddPoints;

        private bool precisionMode = false;
        private bool dragMode = false;
        private Point previousPosition;
        private State state = State.Idle;
        private Axes axes = new Axes();
        private readonly LineModel linemodel = new LineModel() { Count = 2, ScreenOrReal = true, XOrY = true };
        private readonly List<DataPoint> data = new List<DataPoint>();
        private readonly Crosshair Crossair = new Crosshair();
        private readonly Crosshair CrossairL = new Crosshair();
        private Line LineTool = new Line()
        {
            Stroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xE6, 0x00)),
            Focusable = false,
            IsHitTestVisible = false,
            StrokeThickness = 2,
            Visibility = Visibility.Hidden,
            StrokeDashArray = new DoubleCollection { 5.0, 5.0 },
            StrokeEndLineCap = PenLineCap.Round
        };
        private bool useLineTool = false;
        private string imagepath = null;

        //Zoom properties, prop in percentage
        private double zoom = 2, prop = 100;
        const double MinZoom = 1;
        const double MaxZoom = 16;
        const double MinZoomMain = 10;
        const double MaxZoomMain = 1000;

        //Selection rectangle
        private bool selecting = false; //Selection rectangle off
        private Point selFirstPos; //First rectangle corner
        private Rectangle selRect; //SelectionRectangle

        //Help window
        private Help helpWindow;

        private readonly OpenFileDialog ofd = new OpenFileDialog
        {
            FileName = "", 
            Filter = "Supported files|*.bmp;*.gif;*.tiff;*.jpg;*.jpeg;*.png;*.gdf|Image files|*.bmp;*.gif;*.tiff;*.jpg;*.jpeg;*.png|Graph Digitizer files|*.gdf",
        };

        private readonly SaveFileDialog sfd = new SaveFileDialog
        {
            FileName = "", 
            Filter = "Graph Digitizer Files|*.gdf|Text files|*.txt|CSV files|*.csv",
        };

        public MainWindow()
        {
            this.InitializeComponent();

            ScreenToReal = this.ScreenToRealCoords;
            RealToScreen = this.RealToScreenCoords;
            AddScreenPoint = this.AddPoint;
            AddRealPoint = this.AddPointReal;
            AddPoints = this.AddPointsOfLine;
            this.dgrPoints.ItemsSource = this.data;
            this.axes.Xmin.Value = 0.0;
            this.axes.Xmax.Value = 1.0;
            this.axes.Ymin.Value = 0.0;
            this.axes.Ymax.Value = 1.0;
            this.zoom = MinMax(Properties.Settings.Default.Zoom, MinZoom, MaxZoom);
            this.cnvGraph.Children.Add(CrossairL.X);
            this.cnvGraph.Children.Add(CrossairL.Y);
            this.cnvGraph.Children.Add(LineTool);
            this.cnvZoom.Children.Add(Crossair.X);
            this.cnvZoom.Children.Add(Crossair.Y);

            if (System.IO.File.Exists(Properties.Settings.Default.LastFile))
            {
                this.OpenFile(Properties.Settings.Default.LastFile);
            }
            else
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(Util.Decode(Properties.Settings.Default.LastFile)))
                    {
                        ReadFromGDF(ms);
                    }
                }
                catch { }
            }

            this.UpdateProportions(MinMax(Properties.Settings.Default.Proportion, MinZoomMain, MaxZoomMain));

            svwGraph.ScrollToHorizontalOffset(Properties.Settings.Default.HorizontalOffset);
            svwGraph.ScrollToVerticalOffset(Properties.Settings.Default.VerticalOffset);
            Screenshot.ScreenshotController.SetImageEvent = new Screenshot.ScreenshotController.SetImage(this.SetCurrentImageAndResize);
        }

        private void OpenFile(string path)
        {
            var bmp = new BitmapImage(new Uri(path));
            imagepath = path;

            SetCurrentImage(bmp);
        }

        private void LoadImageFromFile(string path)
        {
            if (!File.Exists(path))
                return;
            switch (System.IO.Path.GetExtension(path).ToLower())
            {
                case ".bmp":
                case ".gif":
                case ".tiff":
                case ".jpg":
                case ".jpeg":
                case ".png":
                    this.OpenFile(path);
                    break;
                case ".gdf":
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        ReadFromGDF(fs);
                        imagepath = null;
                    }
                    break;
            }
        }

        private void OnOpenClicked(object sender, RoutedEventArgs e)
        {
            if (!this.ofd.ShowDialog().Value)
                return;

            LoadImageFromFile(this.ofd.FileName);
            //if (this.ofd.FilterIndex == 1)
            //{
            //    LoadImageFromFile(this.ofd.FileName);
            //}
            //else if (this.ofd.FilterIndex == 2)
            //{
            //    LoadImageFromFile(this.ofd.FileName);
            //}
        }

        private void SetToolTip()
        {
            switch (this.state)
            {
                case State.Idle:
                    this.txtToolTip.Text = Dict("tip_state_idle");
                    break;
                case State.Axes:
                    switch (this.axes.Status)
                    {
                        case 0:
                            this.txtToolTip.Text = Dict("tip_state_axes0");
                            break;
                        case 1:
                            this.txtToolTip.Text = Dict("tip_state_axes1");
                            break;
                        case 2:
                            this.txtToolTip.Text = Dict("tip_state_axes2");
                            break;
                        case 3:
                            this.txtToolTip.Text = Dict("tip_state_axes3");
                            break;
                    }
                    break;
                case State.Points:
                    this.txtToolTip.Text = Dict("tip_state_points");
                    break;
                case State.Select:
                    this.txtToolTip.Text = Dict("tip_state_select");
                    break;
                case State.Line:
                    this.txtToolTip.Text = Dict("tip_state_line");
                    break;
                default:
                    this.txtToolTip.Text = string.Empty;
                    break;
            }
        }

        private void imgGraph_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this.imgGraph);
            if (this.dragMode)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (previousPosition != p)
                    {
                        this.svwGraph.ScrollToHorizontalOffset(this.svwGraph.HorizontalOffset + (previousPosition.X - p.X) * 0.9);
                        this.svwGraph.ScrollToVerticalOffset(this.svwGraph.VerticalOffset + (previousPosition.Y - p.Y) * 0.9);
                        previousPosition = p;
                        return;
                    }
                }
                else
                    SetDragMode(false);
            }
            previousPosition = p;
            if (this.selecting) //Update selection rectangle position
            {
                if (p.X > this.selFirstPos.X)
                    this.selRect.Width = p.X - this.selFirstPos.X;
                else
                {
                    Canvas.SetLeft(this.selRect, p.X);
                    this.selRect.Width = this.selFirstPos.X - p.X;
                }

                if (p.Y > this.selFirstPos.Y)
                    this.selRect.Height = p.Y - this.selFirstPos.Y;
                else
                {
                    Canvas.SetTop(this.selRect, p.Y);
                    this.selRect.Height = this.selFirstPos.Y - p.Y;
                }
            }
            else
                SetCrosshair(p);

            if (this.state == State.Axes)
            {
                switch(this.axes.Status)
                {
                    case 1:
                        this.axes.Xaxis.X2 = p.X;
                        this.axes.Xaxis.Y2 = p.Y;
                        break;
                    case 3:
                        this.axes.Yaxis.X2 = p.X;
                        this.axes.Yaxis.Y2 = p.Y;
                        break;
                }
            }
            else if (this.state == State.Line && this.useLineTool)
            {
                this.LineTool.X2 = p.X;
                this.LineTool.Y2 = p.Y;
            }
            Crossair.Hide(true);
            p.X *= 100.0 / this.prop;
            p.Y *= 100.0 / this.prop;
            this.UpdateStatusCoords(p.X, p.Y);
            Canvas.SetLeft(this.imgZoom, this.cnvZoom.ActualWidth / 2 - p.X * this.zoom);
            Canvas.SetTop(this.imgZoom, this.cnvZoom.ActualHeight / 2 - p.Y * this.zoom);
        }

        private void UpdateStatusCoords(double X, double Y)
        {
            //X *= 100.0 / this.prop;
            //Y *= 100.0 / this.prop;
            this.GetRealCoords(X, Y, out var xreal, out var yreal);
            this.txtScreenX.Text = X.ToString("F2");
            this.txtScreenY.Text = Y.ToString("F2");
            this.txtRealX.Text = FormatNum(xreal);
            this.txtRealY.Text = FormatNum(yreal);
        }

        public static string FormatNum(double num)
        {
            if (double.IsNaN(num)) return "N/A";

            var aux = Math.Abs(num);
            if (aux > 999999.0 || (aux < 0.00001 && aux > 0))
            {
                return num.ToString("E4");
            }

            var dig = (int)(Math.Log10(aux) + Math.Sign(Math.Log10(aux)));
            return num.ToString(dig >= 0 ? $"F{8 - dig}" : "F8");
        }

        private void cnvZoom_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.precisionMode)
            {
                var p = e.GetPosition(this.imgZoom);
                var p2 = e.GetPosition(this.cnvZoom);
                Crossair.SetHorizental(Math.Max(0, Canvas.GetLeft(this.imgZoom)), Math.Min(this.cnvZoom.ActualWidth, Canvas.GetLeft(this.imgZoom) + this.imgZoom.Width), p2.Y);
                Crossair.SetVertical(p2.X, Math.Max(0, Canvas.GetTop(this.imgZoom)), Math.Min(this.cnvZoom.ActualHeight, Canvas.GetTop(this.imgZoom) + this.imgZoom.Height));
                Crossair.Hide(false);
                Crossair.SetState(this.state, this.axes.Status);
                CrossairL.Hide(true);
                this.UpdateStatusCoords(p.X / this.zoom, p.Y / this.zoom);
            }
        }

        private void ZoomModeIn()
        {
            XYPoint prev; 
            Rect r;
            this.precisionMode = true;
            GetCursorPos(out prev);
            this.previousPosition.X = (double)prev.X;
            this.previousPosition.Y = (double)prev.Y;
            var p = this.PointToScreen(this.cnvZoom.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0)));
            var p2 = this.PointToScreen(this.cnvZoom.TransformToAncestor(this).Transform(new System.Windows.Point(this.cnvZoom.ActualWidth, this.cnvZoom.ActualHeight))) - p;
            SetCursorPos((int)p.X + (int)(p2.X / 2), (int)p.Y + (int)(p2.Y / 2));
            var p3 = this.PointToScreen(this.imgZoom.TransformToAncestor(this).Transform(new System.Windows.Point(0, 0)));
            var p4 = this.PointToScreen(this.imgZoom.TransformToAncestor(this).Transform(new System.Windows.Point(this.imgZoom.Width, this.imgZoom.Height))) - p3;
            r.Top = Math.Max((int)p.Y, (int)p3.Y);
            r.Bottom = Math.Min((int)p.Y + (int)p2.Y, (int)p3.Y + (int)p4.Y);
            r.Left = Math.Max((int)p.X, (int)p3.X); 
            r.Right = Math.Min((int)p.X + (int)p2.X, (int)p3.X + (int)p4.X);
            ClipCursor(ref r);
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && this.cnvGraph.IsMouseOver)
            {
                this.ZoomModeIn();
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftAlt) ||
                Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LeftShift) ||
                Keyboard.IsKeyDown(Key.RightShift))
                return;

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.O:
                        this.OnWindowPreviewKeyUp(sender, e);
                        this.OnOpenClicked(sender, e);
                        break;
                    case Key.S:
                        this.OnWindowPreviewKeyUp(sender, e);
                        this.OnSaveClicked(sender, e);
                        break;
                    case Key.B:
                        this.OnWindowPreviewKeyUp(sender, e);
                        this.btnFromClipboard_Click(sender, e);
                        break;
                    case Key.A:
                        this.OnWindowPreviewKeyUp(sender, e);
                        this.btnScreenshot_Click(sender, e);
                        break;
                    default:
                        return;
                }
            }

            switch (e.Key)
            {
                case Key.Delete:
                    this.DeleteSelection(sender, e);
                    break;
                case Key.H:
                    this.OnHelpClicked(sender, e);
                    break;
                case Key.D1:
                    this.OnSelectClicked(sender, e);
                    break;
                case Key.D2:
                    this.OnPointsClicked(sender, e);
                    break;
                case Key.D3:
                    this.OnLineClicked(sender, e);
                    break;
                case Key.D4:
                    this.OnFunctionClicked(sender, e);
                    break;
            }
        }

        private void ZoomModeOut(bool recover = true)
        {
            Rect r;
            this.precisionMode = false;
            r.Top = int.MinValue;
            r.Bottom = int.MaxValue;
            r.Left = int.MinValue;
            r.Right = int.MaxValue;
            ClipCursor(ref r);
            if (recover) SetCursorPos((int)this.previousPosition.X, (int)this.previousPosition.Y);
        }

        private void OnWindowPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && this.precisionMode)
                this.ZoomModeOut();
        }

        private void AddPoint(double x, double y)
        {
            this.GetRealCoords(x, y, out var Xpoint, out var Ypoint);
            this.data.Add(new DataPoint(Xpoint, Ypoint, x, y, this.cnvGraph, this.prop, this.data.Count + 1));
            this.data[this.data.Count - 1].Obj.MouseDown += new MouseButtonEventHandler(this.PointMouseDown);
            this.dgrPoints.Items.Refresh();
        }

        private void AddPointReal(double realx, double realy)
        {
            Vector sv = this.RealToScreenCoords(new Vector(realx, realy));
            this.data.Add(new DataPoint(realx, realy, sv.X, sv.Y, this.cnvGraph, this.prop, this.data.Count + 1));
            this.data[this.data.Count - 1].Obj.MouseDown += new MouseButtonEventHandler(this.PointMouseDown);
            this.dgrPoints.Items.Refresh();
        }

        private void CreateXaxis()
        {
            if (this.axes.Xaxis != null)
            {
                this.cnvGraph.Children.Remove(this.axes.Xaxis);
            }

            if (!CheckCoord(this.axes.Xmin) || !CheckCoord(this.axes.Xmax))
                return;

            this.axes.Xaxis = new Line
            {
                X1 = this.axes.Xmin.X * this.prop / 100.0,
                Y1 = this.axes.Xmin.Y * this.prop / 100.0,
                X2 = this.axes.Xmax.X * this.prop / 100.0,
                Y2 = this.axes.Xmax.Y * this.prop / 100.0,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Triangle
            };

            this.cnvGraph.Children.Add(this.axes.Xaxis);
        }

        private void CreateYaxis()
        {
            if (this.axes.Yaxis != null)
            {
                this.cnvGraph.Children.Remove(this.axes.Yaxis);
            }

            if (!CheckCoord(this.axes.Ymin) || !CheckCoord(this.axes.Ymax))
                return;

            this.axes.Yaxis = new Line
            {
                X1 = this.axes.Ymin.X * this.prop / 100.0,
                Y1 = this.axes.Ymin.Y * this.prop / 100.0,
                X2 = this.axes.Ymax.X * this.prop / 100.0,
                Y2 = this.axes.Ymax.Y * this.prop / 100.0,
                Stroke = Brushes.Blue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection
                {
                    5.0, 5.0
                },
                StrokeEndLineCap = PenLineCap.Round
            };
            this.cnvGraph.Children.Add(this.axes.Yaxis);
        }

        private bool CheckCoord(Coord coord)
        {
            return !double.IsNaN(coord.X) && !double.IsNaN(coord.Y);
        }

        private void SelectPoint(double X, double Y)
        {
            if (this.state == State.Axes)
            {
                switch (this.axes.Status)
                {
                    case 0: //Xmin
                        this.axes.Xmin.X = X;
                        this.axes.Xmin.Y = Y;
                        this.axes.Xmax.X = X;
                        this.axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 1:
                        this.axes.Xmax.X = X;
                        this.axes.Xmax.Y = Y;
                        this.CreateXaxis();
                        break;
                    case 2:
                        this.axes.Ymin.X = X;
                        this.axes.Ymin.Y = Y;
                        this.axes.Ymax.X = X;
                        this.axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                    case 3:
                        this.axes.Ymax.X = X;
                        this.axes.Ymax.Y = Y;
                        this.CreateYaxis();
                        break;
                }
                this.axes.Status++;
                if (this.axes.Status == 4)
                { 
                    this.SelectAxesProp();
                    if (this.axes.Xaxis != null && this.axes.Yaxis != null)
                        PointsClicked();
                }
            }
            else if (this.state == State.Points)
            {
                this.AddPoint(X, Y);
            }
            else if (this.state == State.Line)
            {
                this.SetLineTool(X, Y);
            }
            this.SetToolTip();
        }

        private void UpdateData()
        {
            for (var i = 0; i < this.data.Count; i++)
            {
                this.data[i].Obj.Content = (i + 1) % 100;
            }

            this.dgrPoints.Items.Refresh();
        }

        private void PointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.state != State.Select) return;
            if (e.ChangedButton == MouseButton.Left) //Select mode
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) //Add to current selection. If it was already selected, unselect
                {
                    if (this.dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                        this.dgrPoints.SelectedItems.Remove(((Label)sender).Tag);
                    else
                        this.dgrPoints.SelectedItems.Add(((Label)sender).Tag);
                }
                else if (this.dgrPoints.SelectedItems.Count == 1 && this.dgrPoints.SelectedItems.Contains(((Label)sender).Tag))
                    this.dgrPoints.SelectedItems.Clear();
                else
                {
                    this.dgrPoints.SelectedItems.Clear();
                    this.dgrPoints.SelectedItem = ((Label)sender).Tag;
                }
            }
            else if (e.ChangedButton == MouseButton.Right) //Delete mode
                this.DeleteSelection(sender, e);
        }

        private void DeleteSelection(object sender, EventArgs e)
        {
            foreach (DataPoint dp in this.dgrPoints.SelectedItems)
            {
                this.cnvGraph.Children.Remove(dp.Obj);
                this.data.Remove(dp);
            }

            this.UpdateData();
        }

        private void imgGraph_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(this.imgGraph);
            if (e.ChangedButton == MouseButton.Right)
            {
                SetDragMode(true);
                CrossairL.Hide(true);
            }
            else if (this.state == State.Select)
            {
                if (e.ChangedButton != MouseButton.Left) return;
                this.selecting = true;
                this.selFirstPos = p;
                this.selRect = new Rectangle() { Stroke = new SolidColorBrush(new Color() { ScA = 0.7f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), Fill = new SolidColorBrush(new Color() { ScA = 0.2f, ScR = 0.0f, ScG = 1.0f, ScB = 0.0f }), StrokeThickness = 1.0 };
                this.cnvGraph.Children.Add(this.selRect);
                Canvas.SetLeft(this.selRect, this.selFirstPos.X);
                Canvas.SetTop(this.selRect, this.selFirstPos.Y);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
                this.SelectPoint(p.X / this.prop * 100.0, p.Y / this.prop * 100.0);
        }

        private void imgZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.precisionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this.imgZoom);
                this.SelectPoint(p.X / this.zoom, p.Y / this.zoom);
            }
        }

        private void btnAxes_Click(object sender, RoutedEventArgs e)
        {
            Axes_Click();
        }

        private void Axes_Click()
        {
            this.state = State.Axes;
            this.axes.Status = 0;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void btnAxesProp_Click(object sender, RoutedEventArgs e)
        {
            this.SelectAxesProp();
        }

        private void SelectAxesProp()
        {
            if (this.precisionMode) this.ZoomModeOut(false);
            AxesProp.Show(this, this.axes);

            //Point p;
            //GetCursorPos(out p);

            //The program will try to position the window leaving the mouse in a corner
            //if (p.X + ap.Width > SystemParameters.PrimaryScreenWidth)
            //    ap.Left = SystemParameters.PrimaryScreenWidth - ap.Width;
            //else
            //    ap.Left = p.X;
            //
            //if (p.Y + ap.Height > SystemParameters.WorkArea.Height) //Thresold for the Windows taskbar
            //    ap.Top = SystemParameters.WorkArea.Height - ap.Height;
            //else
            //    ap.Top = p.Y;


            //this.axes = ap.Axes;
        }

        private void SetLineTool(double x, double y)
        {
            double p = this.prop / 100.0;
            if (this.useLineTool)
            {
                this.LineTool.X2 = x * p;
                this.LineTool.Y2 = y * p;
                this.SelectLineProp();
            }
            else
            {
                this.LineTool.Visibility = Visibility.Visible;
                this.LineTool.X1 = x * p;
                this.LineTool.Y1 = y * p;
                this.LineTool.X2 = x * p;
                this.LineTool.Y2 = y * p;
                this.useLineTool = true;
            }
        }

        private void SelectLineProp()
        {
            if (this.state != State.Line || !this.useLineTool) return;
            if (this.precisionMode) this.ZoomModeOut(false);
            this.useLineTool = false;
            LineProp.Show(this, linemodel, LineTool);
        }

        private void AddPointsOfLine()
        {
            double p = 100.0 / this.prop;
            LinearModel lm = new LinearModel(LineTool.X1 * p, LineTool.Y1 * p, LineTool.X2 * p, LineTool.Y2 * p);
            if (this.linemodel.ScreenOrReal)
                lm.Interp(this.linemodel.Count);
            else
                lm.InterpInReal(this.linemodel.Count, this.linemodel.XOrY);
            foreach (Vector v in lm.Points)
                this.AddPoint(v.X, v.Y);
        }

        private void DeletePoints()
        {
            foreach (var dp in this.data)
            {
                this.cnvGraph.Children.Remove(dp.Obj);
            }
            
            this.data.Clear();
            this.dgrPoints.Items.Refresh();
        }

        private void OnDeletePointsClicked(object sender, RoutedEventArgs e)
        {
            this.DeletePoints();
        }

        private void OnZoomInClicked(object sender, RoutedEventArgs e)
        {
            if (this.zoom < MaxZoom) this.zoom *= 2; else this.zoom = MaxZoom;
            this.imgZoom.Width = ((BitmapSource)this.imgZoom.Source).PixelWidth * this.zoom;
            this.imgZoom.Height = ((BitmapSource)this.imgZoom.Source).PixelHeight * this.zoom;
        }

        private void OnZoomOutClicked(object sender, RoutedEventArgs e)
        {
            if (this.zoom > MinZoom) this.zoom /= 2; else this.zoom = MinZoom;
            this.imgZoom.Width = ((BitmapSource)this.imgZoom.Source).PixelWidth * this.zoom;
            this.imgZoom.Height = ((BitmapSource)this.imgZoom.Source).PixelHeight * this.zoom;
        }

        private void UpdateProportions(double newprop)
        {
            UpdateProportions(newprop, new Point(this.imgGraph.Width / 2, this.imgGraph.Height / 2));
        }

        private void UpdateProportions(double newprop, Point p)
        {
            if (newprop < MinZoomMain)
                newprop = MinZoomMain;
            else if (newprop > MaxZoomMain)
                newprop = MaxZoomMain;

            newprop /= this.prop;

            this.cnvGraph.Width *= newprop;
            this.cnvGraph.Height *= newprop;
            this.imgGraph.Width = this.cnvGraph.Width;
            this.imgGraph.Height = this.cnvGraph.Height;

            this.svwGraph.ScrollToHorizontalOffset(this.svwGraph.HorizontalOffset + p.X * (newprop - 1));
            this.svwGraph.ScrollToVerticalOffset(this.svwGraph.VerticalOffset + p.Y * (newprop - 1));

            Line line; Label tb; //tb because originally it was a TextBlock
            for (var i = 1; i < this.cnvGraph.Children.Count; i++) //0 Index always for the imgGraph element
            {
                if (this.cnvGraph.Children[i] is Line)
                {
                    line = (Line)this.cnvGraph.Children[i];
                    line.X1 *= newprop;
                    line.Y1 *= newprop;
                    line.X2 *= newprop;
                    line.Y2 *= newprop;
                }
                else if (this.cnvGraph.Children[i] is Label)
                {
                    tb = (Label)this.cnvGraph.Children[i];
                    Canvas.SetLeft(tb, (Canvas.GetLeft(tb) + 8) * newprop - 8);
                    Canvas.SetTop(tb, (Canvas.GetTop(tb) + 8) * newprop - 8);
                }
            }

            this.prop *= newprop;
        }

        private void OnEnlargeClicked(object sender, RoutedEventArgs e)
        {
            PropChange(true);
        }

        private void OnReduceClicked(object sender, RoutedEventArgs e)
        {
            PropChange(false);
        }

        private void OnResizeClicked(object sender, RoutedEventArgs e)
        {
            AutoResize();
        }

        private void AutoResize()
        {
            if (this.imgGraph.Source == null)
                return;
            if (this.imgGraph.Source is BitmapSource bitmapSource)
                this.UpdateProportions(Math.Min(this.svwGraph.ActualWidth / bitmapSource.PixelWidth, this.svwGraph.ActualHeight / bitmapSource.PixelHeight) * 98.0);
        }

        private void PropChange(bool enlarge, Point p)
        {
            double newprop;
            if (enlarge)
                newprop = Math.Floor(this.prop * 1.2);
            else
                newprop = Math.Floor(this.prop / 1.2);
            this.UpdateProportions(newprop, p);
        }

        private void PropChange(bool enlarge)
        {
            double newprop;
            if (enlarge)
                newprop = Math.Floor(this.prop * 1.2);
            else
                newprop = Math.Floor(this.prop / 1.2);
            this.UpdateProportions(newprop);
        }

        private void OnRestoreClicked(object sender, RoutedEventArgs e)
        {
            this.UpdateProportions(100.0);
            this.prop = 100;
        }

        private void OnCopyClicked(object sender, RoutedEventArgs e)
        {
            var res = string.Empty;
            for (var i = 0; i < this.data.Count; i++)
            {
                res += this.data[i].X + "\t" + this.data[i].Y;
                if (i != this.data.Count) res += Environment.NewLine;
            }

            Clipboard.SetText(res);
        }

        private void dgrPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (DataPoint dp in e.RemovedItems)
                dp.Obj.Style = (Style)Application.Current.FindResource("PointStyle");
            foreach (DataPoint dp in e.AddedItems)
                dp.Obj.Style = (Style)Application.Current.FindResource("PointStyleSel");
        }

        private void OnWindowPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.selecting)
            {
                return;
            }

            if (double.IsNaN(this.selRect.Width) || this.selRect.Width < 1.0 || double.IsNaN(this.selRect.Height) || this.selRect.Height < 1.0)
            {
                //Nothing at the moment
            }
            else
            {
                double left = Canvas.GetLeft(this.selRect), top = Canvas.GetTop(this.selRect), x, y;
                Label tb;
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                    this.dgrPoints.SelectedItems.Clear();
                for (int i = 1; i < this.cnvGraph.Children.Count; i++) //Index = 1 is always the imgGraph element
                {
                    if (this.cnvGraph.Children[i] is Label)
                    {
                        tb = (Label)this.cnvGraph.Children[i];
                        x = Canvas.GetLeft(tb) + 8;
                        y = Canvas.GetTop(tb) + 8;
                        if (x >= left && x <= left + this.selRect.Width && y >= top && y <= top + this.selRect.Height) //Point is within the rectangle
                            this.dgrPoints.SelectedItems.Add((DataPoint)tb.Tag);
                    }
                }
            }
            this.cnvGraph.Children.Remove(this.selRect);
            this.selecting = false;
        }

        private void OnHelpClicked(object sender, RoutedEventArgs e)
        {
            if (this.helpWindow == null || !this.helpWindow.IsLoaded)
            {
                this.helpWindow = new Help();
                this.helpWindow.Owner = this;
                this.helpWindow.ShowDialog();
            }
            else
            {
                this.helpWindow.Focus();
                if (this.helpWindow.WindowState == WindowState.Minimized)
                    this.helpWindow.WindowState = WindowState.Normal;
            }
        }

        private void OnSelectClicked(object sender, RoutedEventArgs e)
        {
            this.state = State.Select;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Arrow;
        }

        private void OnPointsClicked(object sender, RoutedEventArgs e)
        {
            PointsClicked();
        }

        private void PointsClicked()
        {
            this.state = State.Points;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void OnLineClicked(object sender, RoutedEventArgs e)
        {
            if (this.precisionMode) this.ZoomModeOut(false);
            this.LineTool.Visibility = Visibility.Hidden;
            this.useLineTool = false;
            if (this.axes.Xaxis == null || this.axes.Yaxis == null)
            {
                MessageBox.Show(Dict("except_valid2"), "", MessageBoxButton.OK, MessageBoxImage.Information);
                Axes_Click();
                return;
            }
            this.state = State.Line;
            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void OnFunctionClicked(object sender, RoutedEventArgs e)
        {
            if (this.precisionMode) this.ZoomModeOut(false);
            if (this.axes.Xaxis == null || this.axes.Yaxis == null)
            {
                MessageBox.Show(Dict("except_valid2"), "", MessageBoxButton.OK, MessageBoxImage.Information);
                Axes_Click();
                return;
            }

            FuncProp.Show(this);
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (!this.sfd.ShowDialog().Value)
            {
                return;
            }

            switch (this.sfd.FilterIndex)
            {
                case 1:
                    SaveToGDF(this.sfd.OpenFile());
                    break;
                case 2:
                    using (var sw = new System.IO.StreamWriter(this.sfd.OpenFile()))
                    {
                        sw.WriteLine("{0,-22}{1,-22}", "X Value", "Y Value");
                        sw.WriteLine(new string('-', 45));
                        foreach (var p in this.data)
                        {
                            sw.WriteLine("{0,-22}{1,-22}", p.X, p.Y);
                        }

                        sw.Close();
                    }
                    break;
                case 3:
                    using (var sw = new System.IO.StreamWriter(this.sfd.OpenFile()))
                    {
                        var sep = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ListSeparator;
                        sw.WriteLine("X Value" + sep + "Y Value");
                        foreach (var p in this.data)
                        {
                            sw.WriteLine(p.X + sep + p.Y);
                        }

                        sw.Close();
                    }
                    break;
            }
        }

        private void SaveToGDF(Stream stream, bool strict = true)
        {
            using (var bw = new BinaryWriter(stream))
            {
                //var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                if (this.imgGraph.Source == null)
                {
                    bw.Write(false);
                    bw.Write(0);
                    return;
                }
                else if (this.imgGraph.Source is BitmapSource bitmapSource)
                {
                    var bmp = this.BufferFromImage(bitmapSource);
                    if (!strict && bmp.Length > 128 * 1024)
                    {
                        bw.Write(false);
                        if (imagepath == null)
                        {
                            bw.Write(0);
                            return; 
                        }
                        else
                        {
                            byte[] ib = Encoding.Unicode.GetBytes(imagepath);
                            bw.Write(ib.Length);
                            bw.Write(ib);
                        }
                    }
                    else
                    {
                        bw.Write(true);
                        bw.Write(bmp.Length);
                        bw.Write(bmp);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "This file format does not support the type of image you are using.",
                        "Unsupported Image Type",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                //Proportion and zoom
                bw.Write(this.prop);
                bw.Write(this.zoom);

                //X axis
                bw.Write(this.axes.Xmin.X); bw.Write(this.axes.Xmin.Y); bw.Write(this.axes.Xmin.Value);
                bw.Write(this.axes.Xmax.X); bw.Write(this.axes.Xmax.Y); bw.Write(this.axes.Xmax.Value);
                bw.Write(this.axes.XLog);

                //Y axis
                bw.Write(this.axes.Ymin.X); bw.Write(this.axes.Ymin.Y); bw.Write(this.axes.Ymin.Value);
                bw.Write(this.axes.Ymax.X); bw.Write(this.axes.Ymax.Y); bw.Write(this.axes.Ymax.Value);
                bw.Write(this.axes.YLog);

                //Points
                bw.Write(this.data.Count);
                foreach (var p in this.data)
                {
                    bw.Write(p.X);
                    bw.Write(p.Y);
                    bw.Write(p.RealX);
                    bw.Write(p.RealY);
                }

                bw.Close();
                //System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        private void ReadFromGDF(Stream stream)
        {
            using (var br = new BinaryReader(stream))
            {
                //var ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
                //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

                BitmapSource bmp = null;
                bool isbytes = br.ReadBoolean();
                int imglen = br.ReadInt32();
                if (imglen > 0)
                {
                    if (isbytes)
                        bmp = this.ImageFromBuffer(br.ReadBytes(imglen));
                    else
                    {
                        imagepath = Encoding.Unicode.GetString(br.ReadBytes(imglen));
                        if (File.Exists(imagepath))
                            bmp = new BitmapImage(new Uri(imagepath));
                    }
                }
                this.prop = MinMax(br.ReadDouble(), MinZoomMain, MaxZoomMain);
                this.zoom = MinMax(br.ReadDouble(), MinZoom, MaxZoom);

                if (bmp != null)
                {
                    this.imgGraph.Width = bmp.PixelWidth * this.prop * 0.01;
                    this.imgGraph.Height = bmp.PixelHeight * this.prop * 0.01;
                    this.imgGraph.Source = bmp;
                    this.cnvGraph.Width = bmp.PixelWidth * this.prop * 0.01;
                    this.cnvGraph.Height = bmp.PixelHeight * this.prop * 0.01;

                    this.imgZoom.Width = bmp.PixelWidth * this.zoom;
                    this.imgZoom.Height = bmp.PixelHeight * this.zoom;
                    this.imgZoom.Source = bmp;

                    this.axes.Xmin.X = br.ReadDouble(); 
                    this.axes.Xmin.Y = br.ReadDouble(); 
                    this.axes.Xmin.Value = br.ReadDouble();
                    this.axes.Xmax.X = br.ReadDouble(); 
                    this.axes.Xmax.Y = br.ReadDouble(); 
                    this.axes.Xmax.Value = br.ReadDouble();
                    this.axes.XLog = br.ReadBoolean();
                    this.CreateXaxis();

                    this.axes.Ymin.X = br.ReadDouble(); 
                    this.axes.Ymin.Y = br.ReadDouble(); 
                    this.axes.Ymin.Value = br.ReadDouble();
                    this.axes.Ymax.X = br.ReadDouble(); this.axes.Ymax.Y = br.ReadDouble(); this.axes.Ymax.Value = br.ReadDouble();
                    this.axes.YLog = br.ReadBoolean();
                    this.CreateYaxis();

                    this.DeletePoints();
                    var total = br.ReadInt32();
                    this.data.Capacity = total;
                    for (var i = 0; i < total; i++)
                    {
                        this.data.Add(new DataPoint(br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), this.cnvGraph, this.prop, i + 1));
                    }

                    this.dgrPoints.Items.Refresh();

                    this.state = State.Points;
                    this.SetToolTip();
                    this.cnvGraph.Cursor = Cursors.Cross;
                }

                //System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
        }

        public BitmapSource ImageFromBuffer(Byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }

        public Byte[] BufferFromImage(BitmapSource imageSource)
        {
            var ms = new MemoryStream();
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(imageSource));
            enc.Save(ms);
            return ms.ToArray();
        }

        private void btnFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("The Clipboard does not contain a valid image.", "Invalid Clipboard content", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetCurrentImage(Clipboard.GetImage());
        }

        private void SetCurrentImage(BitmapSource bmp)
        {
            //Since everything will be deleted, there is no need for calling UpdateProportions(100.0)
            this.prop = 100;

            this.imgGraph.Width = bmp.PixelWidth;
            this.imgGraph.Height = bmp.PixelHeight;
            this.imgGraph.Source = bmp;
            this.cnvGraph.Width = bmp.PixelWidth;
            this.cnvGraph.Height = bmp.PixelHeight;

            this.imgZoom.Width = bmp.PixelWidth * this.zoom;
            this.imgZoom.Height = bmp.PixelHeight * this.zoom;
            this.imgZoom.Source = bmp;

            this.state = State.Axes;
            this.axes.Status = 0;

            this.DeletePoints();
            if (this.axes.Xaxis != null)
            {
                this.cnvGraph.Children.Remove(this.axes.Xaxis);
            }

            if (this.axes.Yaxis != null)
            {
                this.cnvGraph.Children.Remove(this.axes.Yaxis);
            }

            this.axes.Xmin.X = this.axes.Xmin.Y = this.axes.Xmax.X = this.axes.Xmax.Y = this.axes.Ymin.X = this.axes.Ymin.Y = this.axes.Ymax.X = this.axes.Ymax.Y = double.NaN;

            this.SetToolTip();
            this.cnvGraph.Cursor = Cursors.Cross;
        }

        private void SetCurrentImageAndResize(BitmapSource bmp)
        {
            SetCurrentImage(bmp);
            AutoResize();
        }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            WindowState ws = this.WindowState;
            this.WindowState = WindowState.Minimized;
            //this.Hide();
            Screenshot.ScreenshotController.CaptureScreen();
            this.WindowState = ws;
            //this.Show();
            Screenshot.ScreenshotController.screenshot.Activate();
            Screenshot.ScreenshotController.screenshot.Focus();
        }

        private string Dict(string key)
        {
            return Local.Dict(key);
        }

        private Vector ScreenToRealCoords(Vector p)
        {
            GetRealCoords(p.X, p.Y, out double RealX, out double RealY);
            return new Vector(RealX, RealY);
        }

        private Vector RealToScreenCoords(Vector p)
        {
            double Xaxis, Yaxis;
            if (this.axes.Xmax.Value == this.axes.Xmin.Value)
            {
                Xaxis = this.axes.Xmin.X;
            }
            else
            {
                if (this.axes.XLog)
                    Xaxis = (Math.Log10(p.X) - Math.Log10(this.axes.Xmin.Value)) / (Math.Log10(this.axes.Xmax.Value) - Math.Log10(this.axes.Xmin.Value)) * (this.axes.Xmax.X - this.axes.Xmin.X) + this.axes.Xmin.X;
                else
                    Xaxis = (p.X - this.axes.Xmin.Value) / (this.axes.Xmax.Value - this.axes.Xmin.Value) * (this.axes.Xmax.X - this.axes.Xmin.X) + this.axes.Xmin.X;
            }
            
            if (this.axes.Ymax.Value == this.axes.Ymin.Value)
            {
                Yaxis = this.axes.Ymin.Y;
            }
            else
            {
                if (this.axes.YLog)
                    Yaxis = (Math.Log10(p.Y) - Math.Log10(this.axes.Ymin.Value)) / (Math.Log10(this.axes.Ymax.Value) - Math.Log10(this.axes.Ymin.Value)) * (this.axes.Ymax.Y - this.axes.Ymin.Y) + this.axes.Ymin.Y;
                else
                    Yaxis = (p.Y - this.axes.Ymin.Value) / (this.axes.Ymax.Value - this.axes.Ymin.Value) * (this.axes.Ymax.Y - this.axes.Ymin.Y) + this.axes.Ymin.Y;
            }

            if (this.axes.Xmin.Y == this.axes.Xmax.Y && this.axes.Xmax.X == this.axes.Xmin.X && this.axes.Ymax.X == this.axes.Ymin.X && this.axes.Ymax.Y == this.axes.Ymin.Y)
                return new Vector(Xaxis, Yaxis);
            else if (this.axes.Xmin.Y == this.axes.Xmax.Y && this.axes.Xmax.X == this.axes.Xmin.X)
            {
                LinearModel l1 = new LinearModel(this.axes.Ymin.X, this.axes.Ymin.Y, this.axes.Ymax.X, this.axes.Ymax.Y);
                LinearModel l2 = l1.GetVertical(l1.GetPointAtY(Yaxis));
                LinearModel l3 = l1.GetParallel(new Vector(this.axes.Xmin.X, this.axes.Xmin.Y));
                return l2.GetIntersectionWith(l3);
            }
            else if (this.axes.Ymax.Y == this.axes.Ymin.Y && this.axes.Ymax.X == this.axes.Ymin.X)
            {
                LinearModel l1 = new LinearModel(this.axes.Xmin.X, this.axes.Xmin.Y, this.axes.Xmax.X, this.axes.Xmax.Y);
                LinearModel l2 = l1.GetVertical(l1.GetPointAtX(Xaxis));
                LinearModel l3 = l1.GetParallel(new Vector(this.axes.Ymin.X, this.axes.Ymin.Y));
                return l2.GetIntersectionWith(l3);
            }
            else
            {
                LinearModel l1 = new LinearModel(this.axes.Ymin.X, this.axes.Ymin.Y, this.axes.Ymax.X, this.axes.Ymax.Y);
                LinearModel l2 = new LinearModel(this.axes.Xmin.X, this.axes.Xmin.Y, this.axes.Xmax.X, this.axes.Xmax.Y);
                LinearModel l3 = l2.GetParallel(l1.GetPointAtY(Yaxis));
                LinearModel l4 = l1.GetParallel(l2.GetPointAtX(Xaxis));
                return l3.GetIntersectionWith(l4);
            }
        }

        public void GetRealCoords(double X, double Y, out double RealX, out double RealY)
        {
            double Xaxis, Yaxis;
            double delta = (this.axes.Xmin.Y - this.axes.Xmax.Y) * (this.axes.Ymax.X - this.axes.Ymin.X) + (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y);
            //First: obtain the equivalent point in the X axis and in the Y axis
            if (delta == 0)
            {
                if (this.axes.Xmin.Y == this.axes.Xmax.Y && this.axes.Xmax.X == this.axes.Xmin.X && this.axes.Ymax.X == this.axes.Ymin.X && this.axes.Ymax.Y == this.axes.Ymin.Y)
                {
                    Xaxis = this.axes.Xmin.X;
                    Yaxis = this.axes.Ymin.Y;
                }
                else if (this.axes.Xmin.Y == this.axes.Xmax.Y && this.axes.Xmax.X == this.axes.Xmin.X)
                {
                    Xaxis = this.axes.Xmin.X;
                    Yaxis = ((Y - this.axes.Ymin.Y) * (this.axes.Ymax.X - this.axes.Ymin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y) + X * Math.Pow(this.axes.Ymax.X - this.axes.Ymin.X, 2) + this.axes.Ymin.X * Math.Pow(this.axes.Ymax.Y - this.axes.Ymin.Y, 2)) / (Math.Pow(this.axes.Ymax.Y - this.axes.Ymin.Y, 2) + Math.Pow(this.axes.Ymax.X - this.axes.Ymin.X, 2));
                }
                else
                {
                    Xaxis = ((Y - this.axes.Xmin.Y) * (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Xmax.Y - this.axes.Xmin.Y) + X * Math.Pow(this.axes.Xmax.X - this.axes.Xmin.X, 2) + this.axes.Xmin.X * Math.Pow(this.axes.Xmax.Y - this.axes.Xmin.Y, 2)) / (Math.Pow(this.axes.Xmax.Y - this.axes.Xmin.Y, 2) + Math.Pow(this.axes.Xmax.X - this.axes.Xmin.X, 2));
                    Yaxis = this.axes.Ymin.Y;
                }
            }
            else
            {
                Xaxis = ((this.axes.Ymax.X - this.axes.Ymin.X) * (this.axes.Xmax.X * this.axes.Xmin.Y - this.axes.Xmax.Y * this.axes.Xmin.X) - (this.axes.Xmax.X - this.axes.Xmin.X) * (X * (this.axes.Ymin.Y - this.axes.Ymax.Y) + Y * (this.axes.Ymax.X - this.axes.Ymin.X))) / delta;
                Yaxis = (Y * (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Ymax.Y - this.axes.Ymin.Y) + (this.axes.Xmax.Y - this.axes.Xmin.Y) * (this.axes.Ymax.Y * this.axes.Ymin.X - this.axes.Ymax.X * this.axes.Ymin.Y + X * (this.axes.Ymin.Y - this.axes.Ymax.Y))) / delta;
            }

            if (this.axes.Xmax.X == this.axes.Xmin.X)
            {
                RealX = this.axes.Xmin.Value;
            }
            else
            {
                if (this.axes.XLog)
                    RealX = Math.Pow(10.0, Math.Log10(this.axes.Xmin.Value) + (Xaxis - this.axes.Xmin.X) / (this.axes.Xmax.X - this.axes.Xmin.X) * (Math.Log10(this.axes.Xmax.Value) - Math.Log10(this.axes.Xmin.Value)));
                else
                    RealX = this.axes.Xmin.Value + (Xaxis - this.axes.Xmin.X) / (this.axes.Xmax.X - this.axes.Xmin.X) * (this.axes.Xmax.Value - this.axes.Xmin.Value);
            }

            if (this.axes.Ymax.Y == this.axes.Ymin.Y)
            {
                RealY = this.axes.Ymin.Value;
            }
            else
            {
                if (this.axes.YLog)
                    RealY = Math.Pow(10.0, Math.Log10(this.axes.Ymin.Value) + (Yaxis - this.axes.Ymin.Y) / (this.axes.Ymax.Y - this.axes.Ymin.Y) * (Math.Log10(this.axes.Ymax.Value) - Math.Log10(this.axes.Ymin.Value)));
                else
                    RealY = this.axes.Ymin.Value + (Yaxis - this.axes.Ymin.Y) / (this.axes.Ymax.Y - this.axes.Ymin.Y) * (this.axes.Ymax.Value - this.axes.Ymin.Value);
            }
        }

        private struct XYPoint
        {
            public int X, Y;
        }

        private void File_DragEnter(object sender, DragEventArgs e)
        {
            if ((string[])e.Data.GetData(DataFormats.FileDrop) != null)
                e.Effects |= DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void File_Drop(object sender, DragEventArgs e)
        {
            if ((e.Effects & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                string file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                LoadImageFromFile(file);
            }
        }

        private void cnvGraph_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            PropChange(e.Delta > 0, e.GetPosition(this.imgGraph));
            e.Handled = true;
        }

        private void cnvGraph_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.dragMode)
            {
                if (e.RightButton == MouseButtonState.Released)
                {
                    SetDragMode(false);
                    SetCrosshair(e.GetPosition(this.imgGraph));
                }
            }
        }

        private void SetDragMode(bool drag)
        {
            if (drag == this.dragMode)
                return;
            if (drag)
            {
                this.dragMode = true;
                this.cnvGraph.Cursor = Cursors.SizeAll;
            }
            else
            {
                this.dragMode = false;
                this.cnvGraph.Cursor = this.state == State.Select ? Cursors.Arrow : Cursors.Cross;
            }
        }

        private void SetCrosshair(Point p)
        {
            if (!this.dragMode)
            {
                CrossairL.SetHorizental(0, this.imgGraph.ActualWidth, p.Y);
                CrossairL.SetVertical(p.X, 0, this.imgGraph.ActualHeight);
                CrossairL.Hide(this.state == State.Select);
                CrossairL.SetState(this.state, this.axes.Status);
            }
        }

        private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.dgrPoints.SelectedItems.Count > 0;
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteSelection(sender, e);
        }

        private double MinMax(double value, double min, double max)
        {
            return value <= min ? min : (value > max ? max : value);
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                SaveToGDF(ms, false);
                Properties.Settings.Default.LastFile = Util.Encode(ms.ToArray());
            }
            catch { }
            Properties.Settings.Default.Zoom = this.zoom;
            Properties.Settings.Default.Proportion = this.prop;
            Properties.Settings.Default.HorizontalOffset = svwGraph.HorizontalOffset;
            Properties.Settings.Default.VerticalOffset = svwGraph.VerticalOffset;
            Properties.Settings.Default.Save();
        }

        /*
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.MainWindow.Background = Brushes.Transparent;
                // Obtain the window handle for WPF application
                IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
                HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

                // Get System Dpi
                System.Drawing.Graphics desktop = System.Drawing.Graphics.FromHwnd(mainWindowPtr);
                float DesktopDpiX = desktop.DpiX;
                float DesktopDpiY = desktop.DpiY;

                // Set Margins
                NonClientRegionAPI.MARGINS margins = new NonClientRegionAPI.MARGINS();

                // Extend glass frame into client area
                // Note that the default desktop Dpi is 96dpi. The  margins are
                // adjusted for the system Dpi.
                margins.cxLeftWidth = 0;// Convert.ToInt32(5 * (DesktopDpiX / 96));
                margins.cxRightWidth = 0;// Convert.ToInt32(5 * (DesktopDpiX / 96));
                margins.cyTopHeight = Convert.ToInt32(((int)brdToolBar.Height) * (DesktopDpiX / 96));
                margins.cyBottomHeight = 0;// Convert.ToInt32(5 * (DesktopDpiX / 96));

                int hr = NonClientRegionAPI.DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
                //
                if (hr < 0)
                {
                    //DwmExtendFrameIntoClientArea Failed
                }
            }
            // If not Vista, paint background white.
            catch (DllNotFoundException)
            {
                Application.Current.MainWindow.Background = new SolidColorBrush(Color.FromArgb(0x90, 0xF0, 0xF0, 0xF0));
            }
        }
        */

        private struct Rect
        {
            public int Left, Top, Right, Bottom;
        }
    }

    public enum State
    {
        Idle,
        Axes,
        Select,
        Points,
        Line
    }

    public struct Coord
    {
        public double X, Y, Value;
    }

    public class Commands
    {
        public static RoutedCommand Help;

        static Commands()
        {
            Help = new RoutedCommand("Help", typeof(Commands));
            Help.InputGestures.Add(new KeyGesture(Key.F1));
        }
    }
}
