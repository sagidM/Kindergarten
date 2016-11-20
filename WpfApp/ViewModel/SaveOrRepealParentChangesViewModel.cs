using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class SaveOrRepealParentChangesViewModel : ViewModelBase
    {
        public IRelayCommand CloseWithResultCommand { get; }

        public SaveOrRepealParentChangesViewModel()
        {
            CloseWithResultCommand = new RelayCommand<SavingResult>(Close);
        }

        private void Close(SavingResult result)
        {
            Pipe.SetParameter("saving_result", result);
            Finish();
        }

        public override void OnLoaded()
        {
            ParentType = (Parents) Pipe.GetParameter("parent_type");
            Pipe.SetParameter("saving_result", SavingResult.Cancel);
        }

        public Parents ParentType
        {
            get { return _parentType; }
            set
            {
                if (value == _parentType) return;
                _parentType = value;
                OnPropertyChanged();
            }
        }

        private Parents _parentType;

    }
    public enum SavingResult
    {
        Save, NotSave, Cancel
    }
}