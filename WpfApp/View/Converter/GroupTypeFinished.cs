using System;
using System.Globalization;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    class GroupTypeFinished : IValueConverter
    {
        public object FinishedType { get; set; }
        public object NonFinishedType { get; set; }
        public object Null { get; set; }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var g = (Group)value;
            if (g == null) return Null;
            return (g.GroupType & Groups.Finished) != 0 ? FinishedType : NonFinishedType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
