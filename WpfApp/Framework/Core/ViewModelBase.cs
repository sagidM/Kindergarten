using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WpfApp.Annotations;

namespace WpfApp.Framework.Core
{
    public abstract class ViewModelBase : PipeViewModel, INotifyPropertyChanged, ISaverData
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

        public object this[string key]
        {
            get { return _data[key]; }
            set
            {
                object val;
                if (_data.TryGetValue(key, out val) && val == value) return;
                _data[key] = value;
                OnPropertyChanged(System.Windows.Data.Binding.IndexerName);
            }
        }

        public object this[string key, object defaultValue]
        {
            get
            {
                object result;
                return _data.TryGetValue(key, out result) ? result : /*this[key] =*/ defaultValue;  // save current defaultValue if not exists
            }
            set { this[key] = value; }   // for Mode=TwoWay
        }

        #region ISaverData

        object ISaverData.GetAllData() => _data;
        void ISaverData.SetAllData(object data)
        {
            _data = data == null ? new Dictionary<string, object>() : (IDictionary<string, object>) data;
        }

        private IDictionary<string, object> _data;

        #endregion
    }
}