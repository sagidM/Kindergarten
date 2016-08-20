using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class RangeDoubleConverter : IValueConverter
    {
        private double _maximum = double.MaxValue;
        private double _minimum = double.MinValue;

        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value > _maximum ? _maximum : value; }
        }

        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value < _minimum ? _minimum : value; }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var d = (double)value;
            return d >= Minimum && d <= Maximum;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}