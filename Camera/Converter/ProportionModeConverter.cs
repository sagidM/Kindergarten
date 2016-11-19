using System;
using System.Globalization;
using System.Windows.Data;

namespace Camera.Converter
{
    public class ProportionModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((CameraProportion)value)
            {
                case CameraProportion.Default:
                    return "Default";
                case CameraProportion.Common_3X4:
                    return "3 x 4";
                case CameraProportion.Passport_3_5X4_5:
                    return "3.5 x 4.5";
                case CameraProportion.WidthHD_12X7:
                    return "12 x 7";
                case CameraProportion.HighHD_7X12:
                    return "7 x 12";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}