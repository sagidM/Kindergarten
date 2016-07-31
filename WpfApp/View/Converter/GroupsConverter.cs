using System;
using System.ComponentModel;
using System.Globalization;
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
        public string Finished { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Groups))
            {
                Console.WriteLine("???");
                return null;
            }
            var g = (Groups)value;
            if ((g & Groups.Finished) == Groups.Finished)
            {
                //Add finished string
                //g ^= Groups.Finished;
                return Finished;
            }

            switch (g)
            {
                case Groups.Nursery:
                    return Nursery;
                case Groups.Junior:
                    return Junior;
                case Groups.Middle:
                    return Middle;
                case Groups.Older:
                    return Older;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (string)value;
            Func<string, bool> f = (ss) => s.Equals(ss, StringComparison.OrdinalIgnoreCase);

            if (f(Finished)) return Groups.Finished;
            if (f(Nursery)) return Groups.Nursery;
            if (f(Junior)) return Groups.Junior;
            if (f(Middle)) return Groups.Middle;
            if (f(Older)) return Groups.Older;
            return null;
        }
    }
}