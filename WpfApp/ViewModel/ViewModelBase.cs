using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WpfApp.Annotations;

namespace WpfApp.ViewModel
{
    public interface ICloseableViewModel
    {
        event EventHandler ClosingRequest;
    }

    public abstract class ViewModelBase : INotifyPropertyChanged, ICloseableViewModel
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public static readonly bool IsDesignerMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public event EventHandler ClosingRequest;

        protected void OnClosingRequest()
        {
            ClosingRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}