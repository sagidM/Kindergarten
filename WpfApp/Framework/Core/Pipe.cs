using System.Collections.Generic;

namespace WpfApp.Framework.Core
{
    public class Pipe
    {
        private const bool IsDialogDefault = false;
        public Pipe(bool isDialog = IsDialogDefault) : this(new Dictionary<string, object>(), isDialog)
        {
        }
        public Pipe(IDictionary<string, object> parameters, bool isDialog = IsDialogDefault)
        {
            Parameters = parameters;
            IsDialog = isDialog;
        }

        //public Type ViewModelType { get; set; }

        public IDictionary<string, object> Parameters { get; }
        public bool IsDialog { get; set; }

        public void SetParameter(string key, object value) => Parameters[key] = value;
        public bool TryGetParameter(string key, out object value)
        {
            object p;
            if (!Parameters.TryGetValue(key, out p))
            {
                value = null;
                return false;
            }
            value = p;
            return true;
        }

        public object GetParameter(string key)
        {
            return Parameters[key];
        }

        public T GetParameter<T>(string key, T defaultValue)
        {
            return (T) GetParameter(key, (object)defaultValue);
        }

        public object GetParameter(string key, object defaultValue)
        {
            object o;
            return TryGetParameter(key, out o) ? o : defaultValue;
        }

        public static Pipe Default { get; } = new Pipe();
    }
}
