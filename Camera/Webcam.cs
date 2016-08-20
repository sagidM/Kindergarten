using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Camera
{
    public class Webcam
    {
        public ImageFormat Format { get; set; }

        public Webcam(ImageFormat format)
        {
            Format = format;
        }

        public FilterInfoCollection VideoDevices
        {
            get
            {
                if (_videoDevices == null)
                    UpdateDevices();
                return _videoDevices;
            }
        }

        public bool IsRunning => _videoCaptureDevice != null && _videoCaptureDevice.IsRunning;

        public int SelectedDeviceIndex
        {
            get { return _selectedDeviceIndex; }
            private set
            {
                int old = _selectedDeviceIndex;
                _selectedDeviceIndex = value;
                SelectedIndexChanged?.Invoke(old, value);
            }
        }

        public RotateFlipType RotateFlipType { get; set; }

        public void UpdateDevices()
        {
            string monikerString = null;
            if (IsRunning)
                monikerString = VideoDevices[SelectedDeviceIndex].MonikerString;
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            DevicesUpdated?.Invoke(_videoDevices);

            if (monikerString == null || _videoDevices.Count == 0)
            {
                SelectedDeviceIndex = -1;
                return;
            }

            for (int i = 0; i < _videoDevices.Count; i++)
            {
                if (_videoDevices[i].MonikerString == monikerString)
                {
                    SelectedDeviceIndex = i;
                    return;
                }
            }
            // unreachable
        }

        public void StartCaptureVideo(int deviceIndex = 0)
        {
            SelectedDeviceIndex = deviceIndex;
            _videoCaptureDevice?.Stop();

            _videoCaptureDevice = new VideoCaptureDevice(VideoDevices[deviceIndex].MonikerString);
            _videoCaptureDevice.NewFrame += OnNewWpfFrame;
            _videoCaptureDevice.Start();
        }

        public void StopSignal()
        {
            _videoCaptureDevice.SignalToStop();
        }

        public void Stop()
        {
            _videoCaptureDevice.Stop();
        }

        private void OnNewWpfFrame(object sender, NewFrameEventArgs e)
        {
            if (NewWpfFrame == null) return;

            var ms = new MemoryStream();
            lock (_currentFrameLock)
            {
                _currentFrame = (Bitmap) e.Frame.Clone();
                _currentFrame.RotateFlip(RotateFlipType);
                try
                {
                    _currentFrame.Save(ms, Format);
                }
                catch
                {
                    _currentFrame.Dispose();
                    throw;
                }
            }

            NewWpfFrame(sender, new NewStreamFrameEventArgs(ms));
        }

        public void CaptureCurrentFrame(Stream stream)
        {
            CaptureCurrentFrame(stream, Format);
        }
        public void CaptureCurrentFrame(Stream stream, ImageFormat format)
        {
            lock (_currentFrameLock)
                _currentFrame.Save(stream, format);
        }

        public void CaptureCurrentFrame(string fileName)
        {
            CaptureCurrentFrame(fileName, Format);
        }
        public void CaptureCurrentFrame(string fileName, ImageFormat format, float angle = 0)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (angle == 0)
            {
                lock (_currentFrameLock)
                    _currentFrame.Save(fileName, format);
                return;
            }

            int w;
            int h;
            lock (_currentFrameLock)
            {
                w = _currentFrame.Width;
                h = _currentFrame.Height;
            }
            
            using (Bitmap b = new Bitmap(w, h))
            {
                using (var g = Graphics.FromImage(b))
                {
                    float trw = w/2f;
                    float trh = h/2f;
                    g.TranslateTransform(trw, trh);
                    g.RotateTransform(angle);
                    g.TranslateTransform(-trw, -trh);

                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    lock (_currentFrameLock)
                        g.DrawImage(_currentFrame, 0, 0, w, h);
                }
                b.Save(fileName, format);
            }
        }

        private readonly object _currentFrameLock = new object();

        // ReSharper disable once UnusedMember.Local
        private static Size GetRotatedRectSize(int width, int height, float angle)
        {
            double angleRadian = angle*Math.PI/180;

            double x0 = width/2.0;
            double y0 = height/2.0;
            double u0 = Math.Atan(y0/x0);
            double r = Math.Sqrt(x0*x0 + y0*y0);

            double u1 = u0 + angleRadian;
            double x1 = r * Math.Cos(u1);
            double y1 = r * Math.Sin(u1);
            double u2 = u0 - angleRadian;
            double x2 = r * Math.Cos(u2);
            double y2 = r * Math.Sin(u2);

            return new Size((int) Math.Abs(Math.Max(x1, x2)* 2), (int)Math.Abs(Math.Max(y1, y2) *2));
        }

        public void ShowSettings()
        {
            _videoCaptureDevice.DisplayPropertyPage(IntPtr.Zero);
        }


        public event NewStreamFrameEventHandler NewWpfFrame;
        public event Action<int, int> SelectedIndexChanged;
        public event Action<FilterInfoCollection> DevicesUpdated;


        private VideoCaptureDevice _videoCaptureDevice;
        private Bitmap _currentFrame;
        private FilterInfoCollection _videoDevices;
        private int _selectedDeviceIndex;
    }

    public delegate void NewStreamFrameEventHandler(object sender, NewStreamFrameEventArgs e);

    public class NewStreamFrameEventArgs
    {
        public Stream Stream { get; private set; }

        public NewStreamFrameEventArgs(Stream stream)
        {
            Stream = stream;
        }
    }
}