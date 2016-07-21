using System;
using System.Windows.Input;

namespace WpfApp.Command
{
    public interface IRelayCommand : ICommand
    {
        void NotifyCanExecute(bool can = true);
    }

    public abstract class RelayCommandBase<T> : IRelayCommand
    {
        public Predicate<object> Predicate { get; }

        public T Action { get; set; }

        private bool _lastCanExecute;

        protected RelayCommandBase(T action, Predicate<object> predicate)
        {
            Action = action;
            Predicate = predicate;
        }

        public bool CanExecute(object parameter)
        {
            if (!_can) return false;
            if (Predicate == null) return true;

            bool canExecute = Predicate(parameter);
            if (_lastCanExecute != canExecute && CanExecuteChanged != null)
            {
                _lastCanExecute = canExecute;
                CanExecuteChanged(this, EventArgs.Empty);
            }
            return canExecute;
        }

        private bool _can = true;
        public void NotifyCanExecute(bool can)
        {
            _can = can;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public abstract void Execute(object parameter);
        public event EventHandler CanExecuteChanged;
    }


    public class RelayCommand : RelayCommandBase<Action>
    {
        public RelayCommand(Action action, Predicate<object> predicate = null) : base(action, predicate)
        {
        }
        public override void Execute(object parameter)
        {
            Action();
        }
    }
    public class RelayCommand<T> : RelayCommandBase<Action<T>>
    {
        public RelayCommand(Action<T> action, Predicate<object> predicate = null) : base(action, predicate)
        {
        }
        public override void Execute(object parameter)
        {
            Action((T)parameter);
        }
    }
}