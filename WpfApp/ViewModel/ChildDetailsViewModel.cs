using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
            AddChildToArchiveCommand = new RelayCommand<string>(AddChildToArchive);
            RemoveChildFromArchiveCommand = new RelayCommand(RemoveChildFromArchive);
        }

        public override void OnLoaded()
        {
            CurrentChild = (Child)Pipe.GetParameter("child");
            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentGroup));
            Groups = (ObservableCollection<Group>) Pipe.GetParameter("groups");
            Tarifs = (ObservableCollection<Tarif>) Pipe.GetParameter("tarifs");
            _mainViewModel = (MainViewModel) Pipe.GetParameter("owner");
            _context = (KindergartenContext) Pipe.GetParameter("context");
        }

        private void AddChildToArchive(string note)
        {
            if (CurrentChildIsArchived) throw new InvalidOperationException();

            CurrentChild.LastEnterChild.ExpulsionDate = DateTime.Now;
            _context.SaveChanges();
            OnPropertyChanged(nameof(CurrentChildIsArchived));
        }

        private void RemoveChildFromArchive()
        {
            if (!CurrentChildIsArchived) throw new InvalidOperationException();
            
            CurrentChild.EnterChildren.Add(CurrentChild.LastEnterChild = new EnterChild { EnterDate = DateTime.Now });
            _context.SaveChanges();
            OnPropertyChanged(nameof(CurrentChildIsArchived));
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
            await UpdateMainViewModel();
        }

        public override async void OnFinished()
        {
            if (!_mainIsUpdating)
                await UpdateMainViewModel();
        }

        private bool _mainIsUpdating;
        private async Task UpdateMainViewModel()
        {
            _mainIsUpdating = true;
            await _mainViewModel.UpdateChildrenAsync();
            _mainIsUpdating = false;
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

        private async void UpdateChild()
        {
            _context.SaveChanges();
            await UpdateMainViewModel();
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
            }
        }

        public bool CurrentChildIsArchived => CurrentChild.LastEnterChild.ExpulsionDate.HasValue;

        public Group CurrentGroup
        {
            get { return CurrentChild.Group; }
            set
            {
                CurrentChild.Group = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand UpdateChildCommand {get; private set; }
        public IRelayCommand ChangeGroupCommand { get; private set; }
        public IRelayCommand AddChildToArchiveCommand { get; private set; }
        public IRelayCommand RemoveChildFromArchiveCommand { get; private set; }

        private Child _currentChild;
        private ObservableCollection<Group> _groups;
        private MainViewModel _mainViewModel;
        private KindergartenContext _context;
        private ObservableCollection<Tarif> _tarifs;
    }
}