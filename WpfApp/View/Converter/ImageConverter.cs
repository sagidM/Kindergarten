using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp.Settings;

namespace WpfApp.View.Converter
{
    public class ImageConverter : IValueConverter
    {
        private ImageTypes _imageType;
        private string _path;

        public enum ImageTypes
        {
            Child
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
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DefaultImage;

            var path = _path + (string)value;
            if (!File.Exists(path))
                return DependencyProperty.UnsetValue;

            // copy image (to delete in ChildDetails)
            var uri = new Uri(path, UriKind.Absolute);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = uri;
            image.EndInit();
            return image;
        }

        public ImageSource DefaultImage { get; set; }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}