using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimpleEzIIOController
{
    public class BoolToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush TrueBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Green
        private static readonly SolidColorBrush FalseBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
        private static readonly SolidColorBrush NullBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return NullBrush;

            return (bool)value ? TrueBrush : FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}