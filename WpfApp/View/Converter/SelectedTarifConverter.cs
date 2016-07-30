using System;
using System.Globalization;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class SelectedTarifConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Tarif)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}