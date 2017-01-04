using System;
using System.Globalization;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class SexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var sex = (Sex) value;
            var result = ConvertToString(sex);
            if (result == null)
                throw new ArgumentException("Wrong value sex");
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string sex = ((string) value).ToLowerInvariant();
            switch (sex)
            {
                case "мальчик":
                case "мальчонка":
                case "мужчина":
                case "мужик":
                case "пацан":
                case "boy":
                    return Sex.Male;
                case "девочка":
                case "девчонка":
                case "женщина":
                case "баба":
                case "girl":
                    return Sex.Female;
            }
            return null;
        }

        public static string ConvertToString(Sex sex)
        {
            switch (sex)
            {
                case Sex.Male:
                    return "мужской";
                case Sex.Female:
                    return "женский";
                default:
                    return null;
            }
        }
    }
}