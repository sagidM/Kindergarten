using System;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class AddExpenseViewModel : ViewModelBase
    {
        public AddExpenseViewModel()
        {
            AddExpenseCommand = new RelayCommand(AddExpense);
        }

        private void AddExpense()
        {
            if (MoneyExpense <= 0) return;

            var expense = new Expense
            {
                ExpenseType = (ExpenseType)this["SelectedExpenseType"],
                Money = MoneyExpense,
                Description = DescriptionExpense
            };

            var context = new KindergartenContext();
            context.Expenses.Add(expense);
            context.SaveChanges();
            App.Logger.Trace("New expense added");

            Pipe.SetParameter("added_expense", expense);
            Finish();
        }

        public override void OnLoaded()
        {
            var o = this["SelectedExpenseType", null];
            if (o == null)
                this["SelectedExpenseType"] = ExpenseType.Salary;
            else if (!(o is ExpenseType))
                this["SelectedExpenseType"] = (ExpenseType) (long) o;

            Pipe.SetParameter("added_expense", null);
        }

        public double MoneyExpense
        {
            get { return _moneyExpense; }
            set
            {
                if (value.Equals(_moneyExpense)) return;
                _moneyExpense = value;
                OnPropertyChanged();
            }
        }

        public string DescriptionExpense
        {
            get { return _descriptionExpense; }
            set
            {
                if (value == _descriptionExpense) return;
                _descriptionExpense = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand AddExpenseCommand { get; }


        private double _moneyExpense;
        private string _descriptionExpense;
    }
}