using System.Collections.Generic;
using System.Collections.ObjectModel;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class ChildDetailsViewModel : ViewModelBase
    {
        public ChildDetailsViewModel()
        {
            UpdateChildCommand = new RelayCommand(UpdateChild);
            ChangeGroupCommand = new RelayCommand(ChangeGroup);
        }

        private async void ChangeGroup()
        {
            var parameters = new Dictionary<string, object>(3)
            {
                ["child"] = CurrentChild,
                ["groups"] = Groups,
            };
            var pipe = new Pipe(parameters, true);
            StartViewModel<ChangeGroupChildViewModel>(pipe);
            if (!(bool) pipe.GetParameter("saved_new_group"))
                return;

            var group = (Group)pipe.GetParameter("group_result");
            CurrentGroup = group;
            await _mainViewModel.UpdateChildrenAsync();
        }

        public ObservableCollection<Tarif> Tarifs
        {
            get { return _tarifs; }
            set
            {
                if (Equals(value, _tarifs)) return;
                _tarifs = value;
                OnPropertyChanged();
            }
        }

        public override void OnLoaded()
        {
            CurrentChild = (Child)Pipe.GetParameter("child");
            OnPropertyChanged(nameof(CurrentGroup));
            Groups = (ObservableCollection<Group>) Pipe.GetParameter("groups");
            Tarifs = (ObservableCollection<Tarif>) Pipe.GetParameter("tarifs");
            _mainViewModel = (MainViewModel) Pipe.GetParameter("owner");
            _context = (KindergartenContext) Pipe.GetParameter("context");
        }

        private async void UpdateChild()
        {
            _context.SaveChanges();
            await _mainViewModel.UpdateChildrenAsync();
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
        public Child CurrentChild
        {
            get { return _currentChild; }
            set
            {
                if (Equals(value, _currentChild)) return;
                _currentChild = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBobodyOption));
            }
        }

        public bool IsBobodyOption
        {
            get { return (CurrentChild.Options & ChildOptions.IsNoBody) != 0; }
            set
            {
                if (value)
                    CurrentChild.Options |= ChildOptions.IsNoBody;
                else
                    CurrentChild.Options &= ~ChildOptions.IsNoBody;
                OnPropertyChanged();
            }
        }

        public Group CurrentGroup
        {
            get { return CurrentChild.Group; }
            set
            {
                CurrentChild.Group = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand UpdateChildCommand{get; private set; }
        public IRelayCommand ChangeGroupCommand { get; private set; }

        private Child _currentChild;
        private ObservableCollection<Group> _groups;
        private MainViewModel _mainViewModel;
        private KindergartenContext _context;
        private ObservableCollection<Tarif> _tarifs;
    }
}