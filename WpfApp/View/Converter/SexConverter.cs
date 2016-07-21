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
            switch (sex)
            {
                case Sex.Male:
                    return "мальчик";
                case Sex.Female:
                    return "девочка";
            }
            throw new ArgumentException("Wrong value sex");
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
    }
}