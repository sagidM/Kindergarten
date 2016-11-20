using System.Linq;
using System.Threading.Tasks;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class ChangeGroupGroupTypeViewModel : ViewModelBase
    {
        public ChangeGroupGroupTypeViewModel()
        {
            ChangeGroupTypeCommand = new RelayCommand(ChangeGroupType);
        }


        private async void ChangeGroupType()
        {
            ChangeGroupTypeCommand.NotifyCanExecute(false);
            var groupType = await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var group = context.Groups.First(g => g.Id == CurrentGroup.Id);
                group.GroupType = CurrentGroup.GroupType | (CurrentGroupType & Groups.Finished); // save Finished flag
                context.SaveChanges();
                return group.GroupType;
            });
            ChangeGroupTypeCommand.NotifyCanExecute(true);

            Pipe.SetParameter("group_type_result", groupType);
            _finished = true;
            Finish();
        }

        public override void OnFinished()
        {
            if (_finished) return;
            Pipe.SetParameter("group_type_result", null);
            CurrentGroup.GroupType = CurrentGroupType;
        }

        public override void OnLoaded()
        {
            CurrentGroup = (Group) Pipe.GetParameter("group");
            CurrentGroupType = CurrentGroup.GroupType;
        }

        public Group CurrentGroup
        {
            get { return _currentGroup; }
            set
            {
                if (Equals(value, _currentGroup)) return;
                _currentGroup = value;
                OnPropertyChanged();
            }
        }

        public Groups CurrentGroupType
        {
            get { return _currentGroupType; }
            set
            {
                if (value == _currentGroupType) return;
                _currentGroupType = value;
                OnPropertyChanged();
            }
        }
        

        public IRelayCommand ChangeGroupTypeCommand { get; set; }


        private Group _currentGroup;
        private Groups _currentGroupType;
        private bool _finished;
    }
}