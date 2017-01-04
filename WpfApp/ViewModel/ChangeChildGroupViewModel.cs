using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class ChangeChildGroupViewModel : ViewModelBase
    {
        public ChangeChildGroupViewModel()
        {
            SaveCommand = new RelayCommand(Save);
        }

        private async void Save()
        {
            if (CurrentGroup == SelectedGroup) return;

            SaveCommand.NotifyCanExecute(false);

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var child = context.Children.First(c => c.Id == _currentChild.Id);
                child.GroupId = SelectedGroup.Id;

                context.SaveChanges();
            });

            SaveCommand.NotifyCanExecute(true);
            Pipe.SetParameter("saved_group_result", SelectedGroup);
            Finish();
        }

        public override void OnLoaded()
        {
            Groups = (IEnumerable<Group>) Pipe.GetParameter("groups");
            _currentChild = (Child) Pipe.GetParameter("child");
            var gr = (Group) Pipe.GetParameter("current_group");
            SelectedGroup = CurrentGroup = gr;
            Pipe.SetParameter("saved_group_result", null);
        }

        public IEnumerable<Group> Groups
        {
            get { return _groups; }
            set
            {
                if (Equals(value, _groups)) return;
                _groups = value;
                OnPropertyChanged();
            }
        }

        public Group SelectedGroup
        {
            get { return _selectedGroup; }
            set
            {
                if (Equals(value, _selectedGroup)) return;
                _selectedGroup = value;
                OnPropertyChanged();
            }
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

        public IRelayCommand SaveCommand { get; set; }


        private IEnumerable<Group> _groups;
        private Group _selectedGroup;
        private Child _currentChild;
        private Group _currentGroup;
    }
}