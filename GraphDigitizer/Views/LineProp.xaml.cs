using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using GraphDigitizer.Models;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for AxesProp.xaml
    /// </summary>
    public partial class LineProp : Window
    {
        private readonly LineModel LineModel;
        public static bool IsClosed = true;
        private static LineProp gLineProp;
        private static Line gLine;

        public static void Show(Window owner, LineModel lm, Line line)
        {
            gLine = line;
            if (!IsClosed)
            {
                if (gLineProp != null)
                    gLineProp.Focus();
            }
            else
            {
                gLineProp = new LineProp(lm);
                gLineProp.Owner = owner;
                gLineProp.Show();
            }
        }

        public LineProp(LineModel lm)
        {
            this.InitializeComponent();
            LineModel = lm;
            MainGrid.DataContext = LineModel;
            IsClosed = false;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(this.tb1))
            {
                this.tb.Text = Local.Dict("except_valid1");
                return;
            }
            //this.DialogResult = true;
            Util.AddPoints();
            this.Close();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            IsClosed = true;
            if (gLine != null)
                gLine.Visibility = Visibility.Hidden;
        }
    }
}
