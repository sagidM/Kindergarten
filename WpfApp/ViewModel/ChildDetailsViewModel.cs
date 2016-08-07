using System;
using System.Collections.Generic;
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

        public override async void OnLoaded()
        {
            int id = (int)Pipe.GetParameter("child_id");
            _context = new KindergartenContext();

            Child currentChild = null;
            IEnumerable<Group> groups = null;
            IEnumerable<Tarif> tarifs = null;
            MainViewModel mainViewModel = null;

            await Task.Run(() =>
            {
                currentChild = _context.Children
                    .Include("Person")
                    .Include("Group")
                    .First(c => c.Id == id);
                groups = (IEnumerable<Group>) Pipe.GetParameter("groups");
                tarifs = (IEnumerable<Tarif>) Pipe.GetParameter("tarifs");
                mainViewModel = (MainViewModel)Pipe.GetParameter("owner");
            });

            CurrentChild = currentChild;
            Groups = groups;
            Tarifs = tarifs;
            _mainViewModel = mainViewModel;

            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentGroup));
            OnPropertyChanged(nameof(SelectedTarif));
            OnPropertyChanged(nameof(CurrentChild));
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
            StartViewModel<ChangeChildGroupViewModel>(pipe);
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

        private async void UpdateChild()
        {
            _context.SaveChanges();
            await UpdateMainViewModel();
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

        public Tarif SelectedTarif
        {
            get { return Tarifs.First(t => t.Id == CurrentChild.TarifId); }
            set
            {
                if (CurrentChild.TarifId == value.Id) return;
                CurrentChild.TarifId = value.Id;
                OnPropertyChanged();
            }
        }

        public IEnumerable<Tarif> Tarifs
        {
            get { return _tarifs; }
            set
            {
                if (Equals(value, _tarifs)) return;
                _tarifs = value;
                OnPropertyChanged();
            }
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

        public IRelayCommand UpdateChildCommand {get; private set; }
        public IRelayCommand ChangeGroupCommand { get; private set; }
        public IRelayCommand AddChildToArchiveCommand { get; private set; }
        public IRelayCommand RemoveChildFromArchiveCommand { get; private set; }

        private Child _currentChild;
        private MainViewModel _mainViewModel;
        private IEnumerable<Group> _groups;
        private IEnumerable<Tarif> _tarifs;
        private KindergartenContext _context;
    }
}