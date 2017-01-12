using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfApp.Annotations;

namespace WpfApp.Framework.Core
{
    public class DirtyPropertyChangeNotifier : INotifyPropertyChanged
    {
        public bool IsTracking { get; private set; }
        public int DirtyFieldCount => _dirtyValues.Count;
        public bool HasDirty => DirtyFieldCount != 0;

        private readonly IDictionary<string, object> _firstValues = new Dictionary<string, object>();
        private readonly IDictionary<string, object> _dirtyValues = new Dictionary<string, object>();

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged(object newValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (!IsTracking) return;

            object oldValue;
            if (_firstValues.TryGetValue(propertyName, out oldValue))
            {
                var oldCount = _dirtyValues.Count;
                if (Equals(newValue, oldValue))
                {
                    // remove dirty
                    if (_dirtyValues.Remove(propertyName)) RemovedDirty?.Invoke(propertyName);
                }
                else
                {
                    // add dirty
                    if (!_dirtyValues.ContainsKey(propertyName))
                    {
                        _dirtyValues.Add(propertyName, newValue);
                        AddedDirty?.Invoke(propertyName);
                    }
                    else
                    {
                        _dirtyValues[propertyName] = newValue;
                    }
                }

                var newCount = _dirtyValues.Count;
                if (oldCount != newCount) DirtyCountChanged?.Invoke();
            }
            else
            {
                // first initialisation
                _firstValues.Add(propertyName, newValue);
            }
        }

        public void SetProperty(string propertyName, object value)
        {
            _firstValues[propertyName] = value;
        }

        public void ClearDirties()
        {
            var hadDirty = _dirtyValues.Count != 0;
            foreach (var pair in _dirtyValues)
                _firstValues[pair.Key] = pair.Value;
            _dirtyValues.Clear();
            if (hadDirty)
            {
                DirtyCountChanged?.Invoke();
            }
        }

        public void StartTracking()
        {
            IsTracking = true;
        }

        public void StopTracking()
        {
            IsTracking = false;
        }

        public event Action<string> AddedDirty;
        public event Action<string> RemovedDirty;
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action DirtyCountChanged;

        public bool WasPropertyChanged(string propertyName)
        {
            object value;
            return _dirtyValues.TryGetValue(propertyName, out value) && _firstValues[propertyName] != value;
        }
    }
}