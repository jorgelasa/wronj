using System.Globalization;

namespace WRONJ.Toolkit
{
    public class DoubleToStringConverter : IValueConverter
    {
        public bool OnlyPositive { get; set; }
        public int Decimals { get; set; } = -1;
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (OnlyPositive && (value is double d && d <= 0))
                return string.Empty;

            if (Decimals >= 0)
            {
                string format = $"{{0:F{Decimals}}}";
                return string.Format(format, value);
            }
            return value?.ToString();
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!Double.TryParse((string?)value, out double dVal))
                dVal = 0;
            return dVal;
        }
    }
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        {
            if ((int)value <= 0)
                return string.Empty;
            return value.ToString();
        }
        public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        {
            if (!Int32.TryParse((string)value, out int dVal))
                dVal = 0;
            return dVal;
        }
    }
}
