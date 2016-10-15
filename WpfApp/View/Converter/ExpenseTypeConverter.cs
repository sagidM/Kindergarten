using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class ExpenseTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ExpenseType)) return DependencyProperty.UnsetValue;

            switch ((ExpenseType)value)
            {
                case ExpenseType.Salary:
                    return "Зарплата";
                case ExpenseType.Tax:
                    return "Налоги";
                case ExpenseType.Foodstuff:
                    return "Продукы питания";
                case ExpenseType.Utilities:
                    return "Коммунальные услуги";
                case ExpenseType.Private:
                    return "Личное";
                case ExpenseType.Other:
                    return "Прочее";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}