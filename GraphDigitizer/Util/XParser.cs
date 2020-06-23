using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphDigitizer
{
    public class XParser
    {
        public XParser() { LastException = null; }

        public List<double> ParseDataList(string expression)
        {
            LastException = null;
            List<double> ret = new List<double>();
            if (expression == null || expression.Length == 0)
            { 
                LastException = new Exception(Local.Dict("except_valid0"));
                return ret;
            }
            string[] parts = expression.Split(new char[] { ',' });
            if (parts == null || parts.Length == 0)
            {
                LastException = new Exception(Local.Dict("except_valid1"));
                return ret;
            }

            foreach (string part in parts)
            {
                if (part.Contains(':'))
                {
                    string[] subp = part.Split(new char[] { ':' });
                    switch(subp.Length)
                    {
                        case 2:
                            if (double.TryParse(subp[0].Trim(), out double min2) && 
                                double.TryParse(subp[1].Trim(), out double max2) &&
                                min2 <= max2)
                            {
                                double resolution = Math.Min(GetMaxResolution(min2), GetMaxResolution(max2));
                                double v = min2;
                                while (v <= max2)
                                {
                                    ret.Add(v);
                                    v += resolution;
                                }
                            }
                            else
                                LastException = new Exception(Local.Dict("except_valid4"));
                            break;
                        case 3:
                            if (double.TryParse(subp[0].Trim(), out double min3) &&
                                double.TryParse(subp[1].Trim(), out double dv) &&
                                double.TryParse(subp[2].Trim(), out double max3))
                            {
                                double v = min3;
                                if (min3 <= max3 && dv > 0)
                                {
                                    while (v <= max3)
                                    {
                                        ret.Add(v);
                                        v += dv;
                                    }
                                }
                                else if (min3 >= max3 && dv < 0)
                                {
                                    while (v >= max3)
                                    {
                                        ret.Add(v);
                                        v += dv;
                                    }
                                }
                                else
                                    LastException = new Exception(Local.Dict("except_valid4"));
                            }
                            else
                                LastException = new Exception(Local.Dict("except_valid4"));
                            break;
                        default:
                            LastException = new Exception(Local.Dict("except_valid4"));
                            break;
                    }
                }
                else if (double.TryParse(part.Trim(), out double pdb))
                {
                    ret.Add(pdb);
                }
                else
                {
                    LastException = new Exception(Local.Dict("except_valid4"));
                }
            }
            return ret;
        }

        public Exception LastException { get; private set; }

        private double GetMaxResolution(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value;
            if (value == 0)
                return 1.0;

            value = Math.Abs(value);
            string s = value.ToString();
            int doti = s.IndexOf('.');
            int i;
            for (i = 0; i < s.Length; ++i)
            {
                if (s[i] > '0' && s[i] <= '9')
                    break;
            }
            if (doti < i)
                s = s.Substring(0, i + 1);
            else
                s = s.Substring(0, i + 1).PadRight(doti, '0');
            if (double.TryParse(s, out double res) && res > 0)
                return res;
            return 1.0;
        }
    }
}
