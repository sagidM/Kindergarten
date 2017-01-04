using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class IncomeFictitionEditId : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value[0] == DependencyProperty.UnsetValue)
                return DependencyProperty.UnsetValue;

            var id = (int)value[0];
            var prefix = (string)value[1];
            return prefix + id;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            var s = (string) value;
            int pref = s.LastIndexOf("-", StringComparison.InvariantCulture);

            int id;
            string prefix;
            if (pref < 0)
            {
                id = 0;
                prefix = s + "-";
            }
            else
            {
                prefix = int.TryParse(s.Substring(pref + 1), out id) ? s.Substring(0, pref + 1) : s + "-";
            }
            return new object[] {id, prefix};
        }
    }
}