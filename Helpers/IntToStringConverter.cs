using System;
using System.Globalization;
using System.Windows.Data;

namespace Back_It_Up.Helpers
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (int.TryParse(value as string, out int intValue))
                return intValue;

            return 0; // Or a default value of your choice
        }
    }
}
