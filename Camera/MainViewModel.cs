using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video.DirectShow;

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

        public static readonly DependencyProperty CamerasProperty = DependencyProperty.Register(
            nameof(Cameras), typeof (IEnumerable<string>), typeof (MainViewModel), new PropertyMetadata(default(string)));

        public IEnumerable<string> Cameras
        {
            get { return (IEnumerable<string>) GetValue(CamerasProperty); }
            set { SetValue(CamerasProperty, value); }
        }

        private static bool _skipSelectedIndexCameraCallback;
        public static readonly DependencyProperty SelectedIndexCameraProperty = DependencyProperty.Register(
            nameof(SelectedIndexCamera), typeof (int), typeof (MainViewModel), new PropertyMetadata(default(int),
                (o, args) =>
                {
                    if (_skipSelectedIndexCameraCallback)
                    {
                        _skipSelectedIndexCameraCallback = false;
                        return; // skipping
                    }
                    var self = (MainViewModel)o;
                    self._webcam.StartCaptureVideo((int) args.NewValue);
                }));

        public int SelectedIndexCamera
        {
            get { return (int) GetValue(SelectedIndexCameraProperty); }
            set { SetValue(SelectedIndexCameraProperty, value); }
        }

        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            "IsRunning", typeof (bool), typeof (MainViewModel), new PropertyMetadata(default(bool)));

        public bool IsRunning
        {
            get { return (bool) GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
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
        public ICommand UpdateCamerasCommand { get; }
        public ICommand RefreshCameraCommand { get; }
        public ImageFormat ImageFormat { get; set; } = ImageFormat.Jpeg;

        public MainViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _lastInvoke = dispatcher.BeginInvoke(new Action(() => { })); // cap less null ref exception
            TakePictureCommand = new RelayCommand<object>(OnTakePicture);
            ShowSettingsCommand = new RelayCommand<object>(ShowSettings);
            TurnRightCommand = new RelayCommand<object>(OnTurnRight);
            UpdateCamerasCommand = new RelayCommand<object>(OnUpdateCameras);
            RefreshCameraCommand = new RelayCommand<object>(OnRefreshCamera);
            _setImage = new Action<ImageSource>(b =>
            {
                ImageSource = b;
            });

            _webcam = new Webcam(ImageFormat.Bmp);
            _webcam.NewWpfFrame += _webcam_NewWpfFrame;
            _webcam.DevicesUpdated += _webcam_DevicesUpdated;
            _webcam.SelectedIndexChanged += _webcam_SelectedIndexChanged;

            OnRefreshCamera(null);
        }

        private void OnRefreshCamera(object o)
        {
            IsRunning = false;

            if (_webcam.IsRunning) _webcam.Stop();
            _webcam.UpdateDevices();
            var devices = _webcam.VideoDevices;
            if (devices.Count > 0)
            {
                _webcam.StartCaptureVideo(0);
                IsRunning = _webcam.IsRunning;
                _webcam_DevicesUpdated(devices);
            }
        }

        private void _webcam_SelectedIndexChanged(int oldValue, int newValue)
        {
            _skipSelectedIndexCameraCallback = true;
            SelectedIndexCamera = newValue;
        }

        private void _webcam_DevicesUpdated(FilterInfoCollection obj)
        {
            Cameras = obj.Cast<FilterInfo>().Select(d => d.Name);
        }

        private void OnUpdateCameras(object o)
        {
            _webcam.UpdateDevices();
            if (_webcam.VideoDevices.Count > 0)
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
            Console.WriteLine(fileName);
            if (!Util.OneMorePhotoLeft())
                CloseRequire?.Invoke(this, EventArgs.Empty);
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

        public void OnClosed()
        {
            if (_webcam.IsRunning) _webcam.StopSignal();
        }

        public event EventHandler CloseRequire;

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
            if (args.Length > 2 && args[2].Length > 0)
            {
                int.TryParse(args[2], out _photoCountDecrement);
                if (_photoCountDecrement > 0)
                    IsInfinityPhotoCount = false;
            }
        }

        private static Func<string, string> _getNewFileName;
        private const string PhotosDirectory = "photos";
        private static readonly string FileNameFormat = Path.Combine(PhotosDirectory, "photo_{0}.{1}"); // 0 - inc, 1 - format

        public static string NewImageFileName(string format)
        {
            return _getNewFileName(format);
        }
        
        private static int _photoCountDecrement;
        private static readonly bool IsInfinityPhotoCount = true;

        public static bool OneMorePhotoLeft()
        {
            if (IsInfinityPhotoCount) return true;
            if (_photoCountDecrement <= 1)
                return false;
            --_photoCountDecrement;
            return  true;
        }
    }
}