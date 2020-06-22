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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LoreSoft.MathExpressions;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// FuncProp.xaml 的交互逻辑
    /// </summary>
    public partial class FuncProp : Window
    {
        public static FuncProp gFuncProp;
        public static bool IsClosed = true;

        public static void Show(Window owner)
        {
            if (!IsClosed)
            {
                if (gFuncProp != null)
                    gFuncProp.Focus();
            }
            else
            {
                gFuncProp = new FuncProp();
                gFuncProp.Owner = owner;
                gFuncProp.Show();
            }
        }

        private static readonly DataClass Data = new DataClass 
        { 
            XExpression = string.Empty, 
            Function = string.Empty, 
            FName = string.Empty, 
            FunctionItems = new NotifyClass.NotifyList<string>(FunctionExpression.GetFunctionNames()), 
            OperatorItems = new NotifyClass.NotifyList<string>(OperatorExpression.GetOperatorNames()), 
            VariableItems = new NotifyClass.NotifyList<string>() 
        };

        private readonly MathEvaluator evaluator;

        public FuncProp()
        {
            InitializeComponent();
            IsClosed = false;
            evaluator = new MathEvaluator();
            maingrid.DataContext = Data;
        }

        private void OnAcceptClick(object sender, RoutedEventArgs e)
        {
            XParser xp = new XParser();
            List<double> dataset = xp.ParseDataList(Data.XExpression);
            
            try
            {
                foreach (double v in dataset)
                {
                    if (cb.SelectedIndex == 0)
                    {
                        evaluator.Variables["x"] = v;
                        Util.AddRealPoint(v, evaluator.Evaluate(Data.Function));
                    }
                    else
                    {
                        evaluator.Variables["y"] = v;
                        Util.AddRealPoint(evaluator.Evaluate(Data.Function), v);
                    }
                }
                tb.Visibility = Visibility.Collapsed;
            }
            catch (Exception exc)
            {
                tb.Text = exc.Message;
                tb.Visibility = Visibility.Visible;
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            Data.VariableItems.Clear();
            Data.VariableItems.AddRange(evaluator.Variables.Keys);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedIndex == 0)
            {
                Data.Function = ReplaceVariable(Data.Function, "y", "x");
                Data.FName = "f(x)";
                evaluator.Variables.Remove("y");
            }
            else
            {
                Data.Function = ReplaceVariable(Data.Function, "x", "y");
                Data.FName = "g(y)"; 
                evaluator.Variables.Remove("x");
            }
        }

        private string ReplaceVariable(string exp, string oldvar, string newvar)
        {
            string ret = "";
            if (exp == null || exp.Length == 0)
                return ret;
            int i = GetNextVarIndex(exp, oldvar);
            while (i >= 0)
            {
                ret += exp.Substring(0, i) + newvar;
                exp = exp.Substring(i + oldvar.Length);
                i = GetNextVarIndex(exp, oldvar);
            }
            ret += exp;
            return ret;
        }

        private int GetNextVarIndex(string exp, string variable, int startindex = 0)
        {
            int i = exp.IndexOf(variable, startindex);
            if (i >= 0 && (i < 1 || !IsValidChar(exp[i - 1])) && (i > (exp.Length - variable.Length - 1) || !IsValidChar(exp[i + variable.Length])))
                return i;
            return -1;
        }

        private bool IsValidChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        public class DataClass : NotifyClass
        {
            private string xexpression;
            public string XExpression { get { return xexpression; } set { xexpression = value; Notify(nameof(XExpression)); } }

            private string function;
            public string Function { get { return function; } set { function = value; Notify(nameof(Function)); } }

            private string fname;
            public string FName { get { return fname; } set { fname = value; Notify(nameof(FName)); } }

            public NotifyList<string> FunctionItems { get; set; }
            public NotifyList<string> OperatorItems { get; set; }
            public NotifyList<string> VariableItems { get; set; }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tv = sender as TreeView;
            if (tv.SelectedItem != null && tv.SelectedItem is string)
            {
                int ss = tb1.SelectionStart;
                tb1.Text = tb1.Text.Substring(0, ss) + (string)tv.SelectedItem + tb1.Text.Substring(ss);
                tb1.SelectionStart = ss + ((string)tv.SelectedItem).Length;
            }
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
        }
    }
}
