using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DAL.Model;

namespace WpfApp.View.Converter
{
    public class InactiveParentMultiConverter : IMultiValueConverter
    {
        public object Common { get; set; }
        public object Inactive { get; set; }

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            var parent = value[0] as Parent;
            if (parent == null) return DependencyProperty.UnsetValue;  // when filter work, value[0] has DisconnectedItem of NamedObject
            var ids = (IList<int>)value[1];
            return ids.IndexOf(parent.Id) < 0 ? Common : Inactive;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}