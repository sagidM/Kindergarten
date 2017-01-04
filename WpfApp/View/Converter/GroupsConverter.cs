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
        public string Junior1 { get; set; }
        public string Junior2 { get; set; }
        public string Middle { get; set; }
        public string Older { get; set; }
        public string Preparatory { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var g = (Groups)value;
            if ((g & Groups.Finished) != 0) g ^= Groups.Finished;  // finished off
            switch (g)
            {
                case Groups.Nursery:
                    return Nursery;
                case Groups.Junior1:
                    return Junior1;
                case Groups.Junior2:
                    return Junior2;
                case Groups.Middle:
                    return Middle;
                case Groups.Older:
                    return Older;
                case Groups.Preparatory:
                    return Preparatory;
                default:
                    throw new NotSupportedException();
            }
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