using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class SingleEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumType = value.GetType();

            if (!Enum.IsDefined(enumType, parameter))
                return DependencyProperty.UnsetValue;

            string p = parameter as string;
            var enumValue = p != null ? Enum.Parse(enumType, p) : parameter;

            return Equals(enumValue, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.Parse(targetType, (string) parameter);
        }
    }
    public class SingleEnumValuesConverter : IValueConverter
    {
        public object True { get; set; }
        public object False { get; set; }

        private readonly IValueConverter _delegateConverter = new SingleEnumConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)_delegateConverter.Convert(value, targetType, parameter, culture) ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}