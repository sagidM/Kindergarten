using System;
using System.Globalization;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class ChildOptionsConverter : IValueConverter
    {
        public object True { get; set; } = true;
        public object False { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mustBe = (ChildOptions)parameter;
            var options = (ChildOptions)value;
            return (options & mustBe) != 0 ? True : False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}