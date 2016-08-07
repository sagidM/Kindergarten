using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class FromBoolConverter : IValueConverter
    {
        public static object Reverce = new object();

        public object True { get; set; } = true;
        public object False { get; set; } = false;
        public object Null { get; set; } = null;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Null;
            return (bool) value == (Reverce != parameter) ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}