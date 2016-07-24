using System;
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
        }
        public override void OnLoaded()
        {
            CurrentChild = (Child)Pipe.GetParameter("child");
            Groups = (ObservableCollection<Group>) Pipe.GetParameter("groups");
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
            }
        }

        public IRelayCommand UpdateChildCommand{get; private set; }

        private Child _currentChild;
        private ObservableCollection<Group> _groups;
        private MainViewModel _mainViewModel;
        private KindergartenContext _context;
    }
}