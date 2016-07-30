using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
// ReSharper disable InconsistentNaming

namespace WpfApp.View.Converter
{
    public class YearsOldConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dt = new DateTime((DateTime.Now - (DateTime)value).Ticks);

            // For example:
            // 1 год 1 месяц 1 день
            // 2 года 2 месяца 2 дня
            // 5 лет 5 месяцев 5 дней
            return new StringBuilder()
                .Append(dt.Year)
                .Append(" ")
                .Append(MakeRussianWord(dt.Year, "год", "года", "лет"))
                .Append(" ")
                .Append(dt.Month)
                .Append(" ")
                .Append(MakeRussianWord(dt.Month, "месяц", "месяца", "месяцев"))
//                .Append(" ")
//                .Append(dt.Day)
//                .Append(" ")
//                .Append(MakeRussianWord(dt.Day, "день", "дня", "дней"))
                .ToString();
        }

        public static string MakeRussianWord(int year, string _1, string _2_3_4, string other)
        {
            if (year == 0)
                return other;
            if (year < 0)
                year = -year;

            int century = year % 100;
            int decMinOne = (year - 1) % 10;  // -1 ==> 0 == 9

            if ((century >= 10 && century <= 20) || (decMinOne >= 4))
            {
                return other;
            }
            return decMinOne == 0 ? _1 : _2_3_4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}