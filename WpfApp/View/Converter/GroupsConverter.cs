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
            if (g == Groups.Nursery)
                return Nursery;
            if (g == Groups.Junior1)
                return Junior1;
            if (g == Groups.Junior2)
                return Junior2;
            if (g == Groups.Middle)
                return Middle;
            if (g == Groups.Older)
                return Older;
            if (g == Groups.Preparatory)
                return Preparatory;
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