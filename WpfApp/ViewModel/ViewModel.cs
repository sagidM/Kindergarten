using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DAL.Model;
using WpfApp.Command;
using WpfApp.Service;

namespace WpfApp.ViewModel
{
    internal class ViewModel : ViewModelBase
    {
        private readonly KindergartenContext _context = new KindergartenContext();
        public ViewModel()
        {
            UpdateChildCommand = new RelayCommand(UpdateChild);
            FilterDataCommand = new RelayCommand(FilterData);
            ShowAddChildWindowCommand = new RelayCommand(() => AddChildView.Show());
            ShowAddGroupWindowCommand = new RelayCommand(() => AddGroupView.Show());

            PersonFullName = "";

            if (IsDesignerMode) return;

            Load();
        }

        private async void FilterData()
        {
            FilterDataCommand.NotifyCanExecute(false);
            Children = await Task.Run(() =>
            {
                var children = _context.Children.Include(nameof(Child.Person));
                var firsts = PersonFirstName.Split(FullNameSeparators, StringSplitOptions.RemoveEmptyEntries);
                var lasts = PersonLastName.Split(FullNameSeparators, StringSplitOptions.RemoveEmptyEntries);
                var pats = PersonPatronymic.Split(FullNameSeparators, StringSplitOptions.RemoveEmptyEntries);

                IQueryable<Child> res = children;
                if (lasts.Length > 0)
                    res = res.Where(c => lasts.Contains(c.Person.LastName));
                if (firsts.Length > 0)
                    res = res.Where(c => firsts.Contains(c.Person.FirstName));
                if (pats.Length > 0)
                    res = res.Where(c => pats.Contains(c.Person.Patronymic));
                return new ObservableCollection<Child>(res);
            });
            FilterDataCommand.NotifyCanExecute(true);
        }

        private IWindowService _addGroupView;
        public IWindowService AddGroupView => _addGroupView ?? (_addGroupView = WindowServices.AdditionGroupWindow);
        private IWindowService _addChildView;
        public IWindowService AddChildView => _addChildView ?? (_addChildView = WindowServices.AdditionChildWindow);

        private async void Load()
        {
            Children = await Task.Run(() => new ObservableCollection<Child>(
                _context
                .Children
                .Include("Person")
                .Include("Group")));
            Groups = await Task.Run(() => new ObservableCollection<Group>(_context.Groups));
        }

        private async void UpdateChild()
        {
            UpdateChildCommand.NotifyCanExecute(false);
            await Task.Run(() => _context.SaveChanges());
            UpdateChildCommand.NotifyCanExecute(true);
        }

        #region Search

        private string _personFirstName;
        public string PersonFirstName
        {
            get { return _personFirstName; }
            set
            {
                if (value == _personFirstName) return;
                _personFirstName = value;
                OnPropertyChanged();
                SetPersonFullName();
            }
        }

        private string _personLastName;
        public string PersonLastName
        {
            get { return _personLastName; }
            set
            {
                if (value == _personLastName) return;
                _personLastName = value;
                OnPropertyChanged();
                SetPersonFullName();
            }
        }

        private string _personPatronymic;
        public string PersonPatronymic
        {
            get { return _personPatronymic; }
            set
            {
                if (value == _personPatronymic) return;
                _personPatronymic = value;
                OnPropertyChanged();
                SetPersonFullName();
            }
        }

        private void SetPersonFullName()
        {
            if (_calledFromPersonFullName) return;

            var lasts = PersonLastName.Split(' ');
            var firsts = PersonFirstName.Split(' ');
            var pats = PersonPatronymic.Split(' ');

            int len = Math.Max(Math.Max(lasts.Length, firsts.Length), pats.Length);
            string[] words = new string[3 * len];
            for (int i = 0; i < len; i++)
            {
                words[i * 3] = i < lasts.Length ? lasts[i] : "";
                words[i * 3 + 1] = i < firsts.Length ? firsts[i] : "";
                words[i * 3 + 2] = i < pats.Length ? pats[i] : "";
            }
            _personFullName = string.Join(" ", words);
            OnPropertyChanged(nameof(PersonFullName));
        }
        
        private bool _calledFromPersonFullName;
        private string _personFullName;
        public string PersonFullName
        {
            get { return _personFullName; }
            set
            {
                if (_calledFromPersonFullName || value == _personFullName) return;
                _personFullName = value;
                OnPropertyChanged();

                var names = value.Split(' ');

                int capacity = names.Length/3+1;
                var last = new List<string>(capacity);
                var first = new List<string>(capacity);
                var pat = new List<string>(capacity);
                for (int i = 2; i < names.Length; i += 3)
                {
                    var l = names[i - 2];
                    var f = names[i - 1];
                    var p = names[i];

                    last.Add(l);
                    first.Add(f);
                    pat.Add(p);
                }

                switch (names.Length % 3)
                {
                    case 0:
                        break;
                    case 1:
                        last.Add(names[names.Length - 1]);
                        break;
                    case 2:
                        last.Add(names[names.Length - 2]);
                        first.Add(names[names.Length - 1]);
                        break;
                }

                _calledFromPersonFullName = true;
                PersonLastName = string.Join(" ", last);
                PersonFirstName = string.Join(" ", first);
                PersonPatronymic = string.Join(" ", pat);
                _calledFromPersonFullName = false;
            }
        }

        #endregion


        private ObservableCollection<Group> _groups;
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

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Child> _children;
        private static readonly char[] FullNameSeparators = { ' ' };

        public ObservableCollection<Child> Children
        {
            get { return _children; }
            private set
            {
                if (Equals(value, _children)) return;
                _children = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand FilterDataCommand { get; }
        public IRelayCommand UpdateChildCommand { get; }
        public IRelayCommand ShowAddGroupWindowCommand { get; }
        public IRelayCommand ShowAddChildWindowCommand { get; }
    }
}