using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Camera
{
    public class MainViewModel : DependencyObject, IViewModel
    {
        private readonly Dispatcher _dispatcher;

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            nameof(ImageSource), typeof (ImageSource), typeof (MainViewModel), new PropertyMetadata(default(ImageSource)));

        public ImageSource ImageSource
        {
            get { return (ImageSource) GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageAngleProperty = DependencyProperty.Register(
            nameof(ImageAngle), typeof (float), typeof (MainViewModel), new PropertyMetadata(default(float)));

        public float ImageAngle
        {
            get { return (float) GetValue(ImageAngleProperty); }
            set { SetValue(ImageAngleProperty, value); }
        }

        #region Rotate and flip

        public static readonly DependencyProperty IsFlipXProperty = DependencyProperty.Register(
            nameof(IsFlipX), typeof (bool), typeof (MainViewModel),
            new PropertyMetadata(default(bool), OnFlipPropertyChanged));

        public bool IsFlipX
        {
            get { return (bool) GetValue(IsFlipXProperty); }
            set { SetValue(IsFlipXProperty, value); }
        }
        
        private static void OnFlipPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = (MainViewModel)sender;
            RotateFlipType type = self._rotateType;
            if (self.IsFlipX) type |= RotateFlipType.RotateNoneFlipX;
            self._webcam.RotateFlipType = type;
        }

        #endregion

        public ICommand TakePictureCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand TurnRightCommand { get; }
        public ImageFormat ImageFormat { get; set; } = ImageFormat.Jpeg;

        public MainViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _lastInvoke = dispatcher.BeginInvoke(new Action(() => { })); // cap less null ref exception
            TakePictureCommand = new RelayCommand<object>(OnTakePicture);
            ShowSettingsCommand = new RelayCommand<object>(ShowSettings);
            TurnRightCommand = new RelayCommand<object>(OnTurnRight);
            _webcam = new Webcam(ImageFormat.Bmp);
            _setImage = new Action<ImageSource>(b =>
            {
                ImageSource = b;
            });

            _webcam.NewWpfFrame += _webcam_NewWpfFrame;
            _webcam.StartCaptureVideo(0);
        }

        private RotateFlipType _rotateType = RotateFlipType.RotateNoneFlipNone;
        private void OnTurnRight(object obj)
        {
            _rotateType = (RotateFlipType) (((int)_rotateType+1) % 4);
            OnFlipPropertyChanged(this, EmptyEventArgs);
        }

        private void ShowSettings(object o)
        {
            _webcam.ShowSettings();
        }

        private void OnTakePicture(object o)
        {
            // ToString returns capitalize string format
            string fileName = Path.GetFullPath(Util.NewImageFileName(ImageFormat.ToString().ToLowerInvariant()));
            _webcam.CaptureCurrentFrame(fileName, ImageFormat, ImageAngle);
        }

        private void _webcam_NewWpfFrame(object sender, NewStreamFrameEventArgs e)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = e.Stream;
            image.EndInit();
            image.Freeze();

            _lastInvoke.Wait();
            _lastInvoke = _dispatcher.BeginInvoke(_setImage, image);
        }

        public void Close()
        {
            _webcam.StopSignal();
        }

        private readonly Webcam _webcam;
        private readonly Delegate _setImage;
        private DispatcherOperation _lastInvoke;
        private static readonly DependencyPropertyChangedEventArgs EmptyEventArgs = new DependencyPropertyChangedEventArgs();
    }

    internal static class Util
    {
        private static long _currentPhotoInc = 0;

        static Util()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1].Length > 0)
            {
                var nameWithoutExtension = args[1] + ".";
                _getNewFileName = f => nameWithoutExtension + f;
            }
            else
            {
                Directory.CreateDirectory(PhotosDirectory);
                _getNewFileName = f =>
                {
                    while (true)
                    {
                        var name = string.Format(FileNameFormat, ++_currentPhotoInc, f);
                        if (!File.Exists(name)) return name;
                    }
                };
            }
        }

        private static Func<string, string> _getNewFileName;
        private const string PhotosDirectory = "photos";
        private static readonly string FileNameFormat = Path.Combine(PhotosDirectory, "photo_{0}.{1}"); // 0 - inc, 1 - format

        public static string NewImageFileName(string format)
        {
            return _getNewFileName(format);
        }
    }
}