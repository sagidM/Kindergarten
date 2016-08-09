using System;
using System.Globalization;
using System.Windows.Data;

namespace Camera
{
    class ImageAngleConverter : IValueConverter
    {
        private float _lastValue;
        private string _lastValueStr;
        public float MaxValue { get; set; } = float.MaxValue;
        public float MinValue { get; set; } = float.MinValue;

        public ImageAngleConverter()
        {
            _lastValueStr = _lastValue.ToString(CultureInfo.InvariantCulture);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float f = (float) value;
            if (Math.Abs(f - _lastValue) < 0.0001) return _lastValueStr;
            _lastValue = f;
            return _lastValueStr = f.ToString(CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string;
            if (s == null) return _lastValue;
            if (s.Length == 0)
            {
                _lastValueStr = "0";
                return 0;
            }
            if (s.IndexOf(' ') >= 0) return _lastValue;

            float f;
            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                return _lastValue;

            if (f < MinValue)
            {
                _lastValueStr = MinValue.ToString(CultureInfo.InvariantCulture);
                return MinValue;
            }
            if (f > MaxValue)
            {
                _lastValueStr = MaxValue.ToString(CultureInfo.InvariantCulture);
                return MaxValue;
            }

            _lastValueStr = s;
            return _lastValue = f;
        }
    }
}
