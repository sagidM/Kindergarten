using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class NullConverter : IValueConverter
    {
        public object Null { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ?? Null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}