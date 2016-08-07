using System;
using System.Collections.ObjectModel;
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
            SaveCommand = new RelayCommand(SaveAsync);
        }

        private async void SaveAsync()
        {
            if (CurrentGroup == SelectedGroup) return;

            SaveCommand.NotifyCanExecute(false);

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var child = context.Children.First(c => c.Id == _currentChild.Id);
                var group = context.Groups.First(g => g.Id == SelectedGroup.Id);
                child.Group = group;

                context.SaveChanges();
            });

            SaveCommand.NotifyCanExecute(true);
            Pipe.SetParameter("group_result", SelectedGroup);
            Pipe.SetParameter("saved_new_group", true);
            Finish();
        }

        public override void OnLoaded()
        {
            Groups = (ObservableCollection<Group>) Pipe.GetParameter("groups");
            _currentChild = (Child) Pipe.GetParameter("child");
            CurrentGroup = SelectedGroup = _currentChild.Group;
            Pipe.SetParameter("saved_new_group", false);
        }

        public ObservableCollection<Group> Groups
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


        private ObservableCollection<Group> _groups;
        private Group _selectedGroup;
        private Child _currentChild;
        private Group _currentGroup;
    }
}