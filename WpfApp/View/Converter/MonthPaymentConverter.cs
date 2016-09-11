using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp.View.Converter
{
    public class MonthPaymentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = (int) value;
            return GetMonthName(index);
        }

        private static string GetMonthName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Январь";
                case 1:
                    return "Февраль";
                case 2:
                    return "Март";
                case 3:
                    return "Апрель";
                case 4:
                    return "Май";
                case 5:
                    return "Июнь";
                case 6:
                    return "Июль";
                case 7:
                    return "Август";
                case 8:
                    return "Сентябрь";
                case 9:
                    return "Октябрь";
                case 10:
                    return "Ноябрь";
                case 11:
                    return "Декабрь";
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}