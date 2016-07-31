using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.Util
{
    public class BoolTextConverter : IValueConverter
    {
        public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;
        public string Yes { get; set; }
        public string No { get; set; }
        public object Uknown { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value ? Yes : No;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (string) value;
            if (string.Equals(s, Yes, StringComparison)) return true;
            if (string.Equals(s, No, StringComparison)) return true;
            return Uknown;
        }
    }
}