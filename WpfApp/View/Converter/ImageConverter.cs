using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp.Settings;
using WpfApp.Util;

namespace WpfApp.View.Converter
{
    public class ImageConverter : IValueConverter
    {
        private ImageTypes _imageType;
        private string _path;

        public enum ImageTypes
        {
            Child, Group,
        }

        public ImageTypes ImageType
        {
            get { return _imageType; }
            set
            {
                switch (_imageType = value)
                {
                    case ImageTypes.Child:
                        _path = Path.GetFullPath(AppFilePaths.ChildImages) + Path.DirectorySeparatorChar;
                        break;
                    case ImageTypes.Group:
                        _path = Path.GetFullPath(AppFilePaths.GroupImages) + Path.DirectorySeparatorChar;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DefaultImage;

            var uri = new Uri(_path + (string)value, UriKind.Absolute);
            return new BitmapImage(uri);
        }

        public ImageSource DefaultImage { get; set; }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}