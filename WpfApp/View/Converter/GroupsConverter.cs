using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class GroupsConverter : IValueConverter
    {
        public string Nursery { get; set; }
        public string Junior { get; set; }
        public string Middle { get; set; }
        public string Older { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var g = (Groups)value;
            if ((g & Groups.Nursery) == Groups.Nursery)
                return Nursery;
            if ((g & Groups.Junior) == Groups.Junior)
                return Junior;
            if ((g & Groups.Middle) == Groups.Middle)
                return Middle;
            if ((g & Groups.Older) == Groups.Older)
                return Older;
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupsFinishedConverter : IValueConverter
    {
        public string Finished { get; set; }
        public string NonFinished { get; set; }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var finished = ((Groups)value & Groups.Finished) == Groups.Finished;
            return finished ? Finished : NonFinished;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}