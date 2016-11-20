using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace WpfApp.Framework
{
    public class WindowStateSaver
    {
        private const string Path = "settings.json";

        private readonly string _key;
        private WindowStateSaver(string key, Window window, ISaverData data)
        {
            _key = key;
            _window = window;
            _data = data;
        }

        private void Configure()
        {
            Settings set;
            if (!_settings.TryGetValue(_key, out set))
            {
                _data.SetAllData(new Dictionary<string, object>());
                App.Logger.Info("Configured by default settings: " + _key);
                return;
            }

            var rect = set.Rect;
            _window.Left = rect.Left;
            _window.Top = rect.Top;
            _window.Width = rect.Width;
            _window.Height = rect.Height;
            var state = set.WindowState;
            _window.WindowState = state == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;  // no minimized
            _data.SetAllData(set.OtherSetting);
        }

        // on close each window
        public void Snapshot()
        {
            var rect = new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
            var settings = new Settings { Rect = rect, WindowState = _window.WindowState, OtherSetting = (IDictionary<string, object>) _data.GetAllData()};
            _settings[_key] = settings;
            App.Logger.Trace("Success snapshot (window)");
        }

        // on show each window
        public static WindowStateSaver ConfigureWindow(string windowKey, Window window, ISaverData data)
        {
            var settings = new WindowStateSaver(windowKey, window, data);
            settings.Configure();
            return settings;
        }

        // on start application
        public static InitialisationResult InitializeSettings()
        {
            if (_settings != null) throw new InvalidOperationException("Already initialized");

            if (!File.Exists(Path))
            {
                _settings = new Dictionary<string, Settings>();
                App.Logger.Info("Settings file not found");
                return InitialisationResult.FileNotFound;
            }

            var json = File.ReadAllText(Path);
            try
            {
                _settings = JsonConvert.DeserializeObject<IDictionary<string, Settings>>(json);
            }
            catch (JsonReaderException)
            {
                _settings = new Dictionary<string, Settings>();
                App.Logger.Error("Syntax error in settings file");
                return InitialisationResult.InvalidConfig;
            }
            catch(Exception e)
            {
                App.Logger.Error(e, "Unknown trouble with settings file");
            }
            if (_settings == null) _settings = new Dictionary<string, Settings>();
            App.Logger.Trace("Success initialisation settings");
            return InitialisationResult.Ok;
        }

        // on exit application
        public static void SaveAllSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings);
                File.WriteAllText(Path, json);
            }
            catch (IOException e)
            {
                App.Logger.Error(e, "Cannot save settings");
            }
        }

        public enum InitialisationResult
        {
            InvalidConfig, FileNotFound, Ok
        }


        private struct Settings
        {
            public Rect Rect;
            public WindowState WindowState;
            public IDictionary<string, object> OtherSetting;
        }

        private static IDictionary<string, Settings> _settings;
        private readonly Window _window;
        private readonly ISaverData _data;
    }
}