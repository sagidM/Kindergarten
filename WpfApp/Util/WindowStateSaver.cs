using System;
using System.Collections.Generic;
using System.Windows;

namespace WpfApp.Util
{
    public class WindowStateSaver
    {
        private readonly string _key;
        private WindowStateSaver(string key, Window window)
        {
            _key = key;
            _window = window;
        }

        public void Configure()
        {
            Settings set;
            if (_settings.TryGetValue(_key, out set))
            {
                var rect = set.Rect;
                _window.Left = rect.Left;
                _window.Top = rect.Top;
                _window.Width = rect.Width;
                _window.Height = rect.Height;
            }
            else
            {
//                _settings[_key] = GetSettingsFromConfigInternal(_key);
                _settings[_key] = new Settings {Rect = new Rect(_window.Left, _window.Top, _window.Width, _window.Height)};
            }
        }

        public void Snapshot()
        {
            var rect = new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
            var settings = new Settings { Rect = rect };
            _settings[_key] = settings;
            SaveSettingsToConfigInternal(_key, settings);
        }


        private static IDictionary<string, Settings> _settings = new Dictionary<string, Settings>();
        private readonly Window _window;

        public static WindowStateSaver ConfigureWindow(string windowKey, Window window)
        {
            var settings = new WindowStateSaver(windowKey, window);
            settings.Configure();
            return settings;
        }

        private static Settings GetSettingsFromConfigInternal(string key)
        {
            return new Settings {Rect = new Rect(100, 100, 500, 500)};
        }

        private static void SaveSettingsToConfigInternal(string key, Settings value)
        {
            // save to config
        }

        private struct Settings
        {
            public Rect Rect;
        }
    }
}