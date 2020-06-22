using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GraphDigitizer.Models
{
    public class AxisValidationRule : ValidationRule
    {
        private static readonly ValidationResult DefResult = new ValidationResult(true, null);

        private bool ValidateLogic(string str)
        {
            return Regex.IsMatch(str.Trim().ToLower(), @"^\-*[0-9]+(e\-*[0-9]+)*$");
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!ValidateLogic(value.ToString()))
                return new ValidationResult(false, Local.Dict("except_valid1"));
            return DefResult;
        }
    }

    public class CountValidationRule : ValidationRule
    {
        private static readonly ValidationResult DefResult = new ValidationResult(true, null);

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (uint.TryParse((string)value, out uint res))
            {
                if (res >= 2)
                    return DefResult;
            }
            return new ValidationResult(false, Local.Dict("except_valid1"));
        }
    }

    public class PositiveValidationRule : ValidationRule
    {
        private static readonly ValidationResult DefResult = new ValidationResult(true, null);

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (double.TryParse((string)value, out double res))
            {
                if (res > 0)
                    return DefResult;
            }
            return new ValidationResult(false, Local.Dict("except_valid3"));
        }
    }

    public class XParserValidationRule : ValidationRule
    {
        private static readonly ValidationResult DefResult = new ValidationResult(true, null);
        private static readonly XParser xParser = new XParser();

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            xParser.ParseDataList((string)value);
            if (xParser.LastException != null)
                return new ValidationResult(false, xParser.LastException.Message);
            else
                return DefResult;
        }
    }
}
