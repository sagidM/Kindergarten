using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class FontSizePercentConverter : IValueConverter
    {
        public double DefaultFontSize { get; set; }

        // returns percent (int)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent;
            if (value is double)
                percent = (double) value;
            else if (!double.TryParse((string) value, out percent))
                percent = DefaultFontSize;

            return (int)(percent * 100 / DefaultFontSize);
        }

        // returns font size (double)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int num;
            if (value is int)
                num = (int) value;
            else if (value is double)
                num = (int)(double) value;
            else if (!int.TryParse((string) value, out num))
                num = 100;

            return num * DefaultFontSize / 100;
        }
    }
}