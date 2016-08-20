using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class DateTimeConverter : IValueConverter
    {
        public string Format { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var defaultString = parameter as string;
            DateTime dt;
            if (defaultString == null)
            {
                dt = (DateTime) value;
            }
            else
            {
                var d = value as DateTime?;
                if (!d.HasValue) return defaultString;
                dt = d.Value;
            }

            return dt.ToString(Format, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dateStr = value as string;
            if (dateStr == null) return DependencyProperty.UnsetValue;
            DateTime result;
            return DateTime.TryParse(dateStr, out result) ? result : DependencyProperty.UnsetValue;
        }
    }
}