using System;
using System.Windows;
using WpfApp.View;

namespace WpfApp.Service
{
    internal class WindowService : IWindowService
    {
        private readonly Func<Window> _getNewWindow;

        public WindowService(Func<Window> getNewWindow)
        {
            _getNewWindow = getNewWindow;
        }

        public bool IsDialog { get; set; }  // adds set

        private Window _view;
        private Window View => _view != null && _view.IsLoaded ? _view : (_view = _getNewWindow());

        public bool? Show()
        {
            if (IsDialog)
                return View.ShowDialog();
            View.Show();
            return null;
        }
    }

    public static class WindowServices
    {
        public static IWindowService AdditionChildWindow { get; } =
            new WindowService(() => new AddChildWindow()) { IsDialog = true };

        public static IWindowService AdditionGroupWindow { get; } =
            new WindowService(() => new AddGroupWindow()) { IsDialog = true };
    }
}