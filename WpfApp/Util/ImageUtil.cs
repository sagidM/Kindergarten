using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace WpfApp.Util
{
    public static class ImageUtil
    {
        private const int WidthRequired = 1280;
        private const int HeightRequired = 720;

        public static BitmapEncoder GetEncoderWithCompressedImage(Uri source)
        {
            var frame = BitmapFrame.Create(source, BitmapCreateOptions.None, BitmapCacheOption.None);
            int width = frame.PixelWidth;
            int height = frame.PixelHeight;
            BitmapImage bitmap;

            if (width <= WidthRequired && height <= HeightRequired)
            {
                bitmap = new BitmapImage(source);
            }
            else
            {
                decimal factorW = (decimal)WidthRequired / width;
                decimal factorH = (decimal)HeightRequired / height;
                decimal factor = Math.Min(factorW, factorH);
                int decWidth = (int)Math.Round(width * factor);
                //int decHeight = (int)Math.Round(height * factor);

                var ms = new MemoryStream(File.ReadAllBytes(source.OriginalString));
                bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.DecodePixelWidth = decWidth;
                //bitmap.DecodePixelHeight = decHeight;
                bitmap.EndInit();
            }

            var encoder = GetEncoder(frame);
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            return encoder;
        }

        public static string SaveImage(string source, string pathWithoutExtension)
        {
            return SaveImage(new Uri(source, UriKind.Absolute), pathWithoutExtension);
        }

        public static string SaveImage(Uri uri, string pathWithoutExtension)
        {
            var encoder = GetEncoderWithCompressedImage(uri);
            string format = GetExtensionFromEncoder(encoder);
            var path = pathWithoutExtension + format;
            using (var fs = new FileStream(path, FileMode.CreateNew))
                encoder.Save(fs);
            return path;
        }

        public static void SaveImage(Uri uri, Stream stream)
        {
            var encoder = GetEncoderWithCompressedImage(uri);
            encoder.Save(stream);
        }

        public static string GetExtensionFromEncoder(BitmapEncoder encoder)
        {
            if (encoder is PngBitmapEncoder)
                return ".png";
            if (encoder is JpegBitmapEncoder)
                return ".jpg";
            if (encoder is GifBitmapEncoder)
                return ".gif";
            if (encoder is BmpBitmapEncoder)
                return ".bmp";
            if (encoder is TiffBitmapEncoder)
                return ".tiff";
            if (encoder is WmpBitmapEncoder)
                return ".wmphoto";
            return null;
        }
        
        private static BitmapEncoder GetEncoder(BitmapSource source)
        {
            var metadata = (BitmapMetadata)source.Metadata;

            // metadata is null provided that image is bmp
            if (source.Format.BitsPerPixel >= 32 || metadata == null || metadata.Format == "gif")
                return new PngBitmapEncoder();
            return new JpegBitmapEncoder();
        }
    }
}