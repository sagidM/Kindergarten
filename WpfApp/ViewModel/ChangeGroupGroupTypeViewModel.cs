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
                group.GroupType = SelectedGroupType | (CurrentGroup.GroupType & Groups.Finished); // save Finished flag
                context.SaveChanges();
                return group.GroupType;
            });

            Pipe.SetParameter("group_type_result", groupType);
            ChangeGroupTypeCommand.NotifyCanExecute(true);
            Finish();
        }

        public override void OnLoaded()
        {
            CurrentGroup = (Group) Pipe.GetParameter("group");
            SelectedGroupType = CurrentGroup.GroupType;
            Pipe.SetParameter("group_type_result", null);
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

        public Groups SelectedGroupType
        {
            get { return _selectedGroupType; }
            set
            {
                if (value == _selectedGroupType) return;
                _selectedGroupType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GroupTypesAreDifferent));
            }
        }

        public bool GroupTypesAreDifferent => _currentGroup != null && _currentGroup.GroupType != _selectedGroupType;

        public IRelayCommand ChangeGroupTypeCommand { get; set; }


        private Group _currentGroup;
        private Groups _selectedGroupType;
    }
}