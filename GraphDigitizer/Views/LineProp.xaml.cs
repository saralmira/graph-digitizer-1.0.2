using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphDigitizer.Models;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for AxesProp.xaml
    /// </summary>
    public partial class LineProp : Window
    {
        private LineModel LineModel;

        public LineProp(LineModel lm)
        {
            this.InitializeComponent();
            LineModel = lm;
            MainGrid.DataContext = LineModel;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(this.tb1))
            {
                this.tb.Text = Local.Dict("except_valid1");
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
