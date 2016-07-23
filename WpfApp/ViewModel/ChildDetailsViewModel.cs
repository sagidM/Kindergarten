using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class ChildDetailsViewModel : ViewModelBase
    {
        private string _s;

        public ChildDetailsViewModel()
        {
            S = "hello";
        }

        public string S
        {
            get { return _s; }
            set
            {
                if (value == _s) return;
                _s = value;
                OnPropertyChanged();
            }
        }
    }
}