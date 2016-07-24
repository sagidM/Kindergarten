using System;
using System.Globalization;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class PaymentSystemConverter : IValueConverter
    {
        public string System1 { get; set; }
        public string System2 { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PaymentSystems)) return null;
            var ps = (PaymentSystems) value;
            switch (ps)
            {
                case PaymentSystems.System1:
                    return System1;
                case PaymentSystems.System2:
                    return System2;
                default:
                    throw new ArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentSystems) return value;
            var s = (string) value;
            Func<string, bool> f = (ss) => s.Equals(ss, StringComparison.OrdinalIgnoreCase);

            if (f(System1)) return PaymentSystems.System1;
            if (f(System2)) return PaymentSystems.System2;
            return null;
        }
    }
}