using System;

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
    }
}