using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

namespace SimpleEzIIOController
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public Brush TrueValue { get; set; } = Brushes.Green;
        public Brush FalseValue { get; set; } = Brushes.Red;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}