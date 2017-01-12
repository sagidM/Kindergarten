using System;
using System.Diagnostics;
using System.IO;

namespace WpfApp.Util
{
    public static class CommonHelper
    {
        private static readonly object UniqueLock = new object();
        private static DateTime _lastTime;
        private static long _unique;
        public static string GetUniqueString()
        {
            lock (UniqueLock)
            {
                var now = DateTime.Now;
                if (_lastTime == now)
                {
                    ++_unique;
                }
                else
                {
                    _unique = 0;
                    _lastTime = now;
                }

                return now.ToString("yyyyMMdd-HHmmss.fffffff_") + _unique;
            }
        }

        /// <example>file.txt => file (i).txt;</example>>
        public static string ChangeFileNameIfFileExists(string path, int i = 2)
        {
            if (!File.Exists(path)) return path;

            int dotPos = path.LastIndexOf('.');
            string file = path.Substring(0, dotPos);    // dir/file
            string ext = path.Substring(dotPos);        // .txt

            string p;
            do
            {
                p = $"{file} ({i++}){ext}";
            } while (File.Exists(p));
            return p;
        }

        public static string GetRightRussianWord(int num, string singular, string plural)
        {
            var m100 = num % 100;
            return m100 >= 10 && m100 <= 20 || m100%10 != 1 ? plural : singular;
        }

        ///<summary>1 рубль, 2 рубля, 5 рублей => <c>GetRightRussianWord(ruble, "рубль", "рубля", "рублей")</c></summary>
        public static string GetRightRussianWord(int num, string s1, string s2, string s5)
        {
            var m100 = num % 100;
            if (m100 >= 10 && m100 <= 20) return s5;

            var m10 = m100 % 10;
            if (m10 == 1) return s1;
            if (m10 > 1 && m10 < 5) return s2;
            return s5;
        }

        public static void OpenFileOrDirectory(string path)
        {
            // #if OS_IS_WINDOWS
            Process.Start("explorer.exe", $"\"{path}\"");
        }
        public static void OpenFileOrDirectoryWithSelected(string path)
        {
            // #if OS_is_Windows
            Process.Start("explorer.exe", $"/select, \"{path}\"");
        }
    }
}