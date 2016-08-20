using System;

namespace Camera
{
    public interface IViewModel
    {
        void OnClosed();
        event EventHandler CloseRequire;
    }
}