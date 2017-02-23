using System;
using System.Windows;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class ChangeDebtViewModel : ViewModelBase
    {
        public ChangeDebtViewModel()
        {
            OkCommand = new RelayCommand(Ok);
        }

        public override void OnLoaded()
        {
            Pipe.SetParameter("ok", false);
            Debt = _oldDebt = (double) Pipe.GetParameter("debt");
        }

        private void Ok()
        {
            if (Math.Abs(Debt - _oldDebt) < 0.01)
            {
                MessageBox.Show("Новый долг не должен совпадать с текущим долгом");
                return;
            }

            Pipe.SetParameter("ok", true);
            Pipe.SetParameter("debt", Debt);
            Pipe.SetParameter("description", Description.Trim());
            Finish();
        }

        public double Debt
        {
            get { return _debt; }
            set
            {
                if (value.Equals(_debt)) return;
                _debt = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand OkCommand { get; }
        private double _debt;
        private double _oldDebt;
        private string _description = "Перерасчёт. ";
    }
}
