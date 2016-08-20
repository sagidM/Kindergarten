using System;
using System.Globalization;
using System.Windows.Data;

namespace Camera.Converter
{
    class BoolConverter : IValueConverter
    {
        public object True { get; set; } = true;
        public object False { get; set; } = false;


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, parameter) ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
