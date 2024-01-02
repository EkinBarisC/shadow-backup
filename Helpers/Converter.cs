using System;
using System.Globalization;
using System.Windows.Data;

namespace Back_It_Up.Helpers
{
    internal class Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null || value == null)
                throw new ArgumentException("Enum and parameter must be set");

            if (!Enum.IsDefined(value.GetType(), value))
                throw new ArgumentException("Value must be an Enum");

            var enumValue = Enum.Parse(value.GetType(), parameter.ToString());

            return enumValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                throw new ArgumentException("Parameter must be set");

            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}
