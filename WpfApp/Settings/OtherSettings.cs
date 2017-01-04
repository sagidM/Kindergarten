using System.Configuration;
using System.Text;

namespace WpfApp.Settings
{
    public static class OtherSettings
    {
        public const string DateFormat = "dd.MM.yyyy";
        public const string TimeFormat = "HH:mm:ss";
        /// <summary>Encoding of settings file (see AppConfig)</summary>
        public static readonly Encoding Encoding;

        public static string Str(this double value)
        {
            return value.ToString("0.#");
        }

        static OtherSettings()
        {
            var encodingStr = ConfigurationManager.AppSettings["SettingsFilesEncoding"];
            Encoding = encodingStr == null ? Encoding.UTF8 : Encoding.GetEncoding(encodingStr);
        }
    }
}