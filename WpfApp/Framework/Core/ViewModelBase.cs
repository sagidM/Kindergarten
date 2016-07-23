using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WpfApp.Annotations;

namespace WpfApp.Framework.Core
{
    public abstract class ViewModelBase : PipeViewModel, INotifyPropertyChanged
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


        public override void OnPreInit()
        {
        }

        public override void OnInit()
        {
        }

        public override void OnLoaded()
        {
        }

        public override void OnFinished()
        {
        }

        public override bool OnFinishing()
        {
            return true;
        }
    }
}