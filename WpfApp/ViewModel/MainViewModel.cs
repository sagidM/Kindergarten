using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using DAL.Model;
using WpfApp.Command;
using WpfApp.Framework.Core;
using WpfApp.Service;
using WpfApp.Util;

// ReSharper disable ExplicitCallerInfoArgument

namespace WpfApp.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        private readonly KindergartenContext _context = new KindergartenContext();
        public MainViewModel()
        {
            UpdateChildCommand = new RelayCommand(UpdateChild);
            ShowAddChildWindowCommand = new RelayCommand(() => StartViewModel<AddChildViewModel>(new Pipe(true)));
            ShowAddGroupWindowCommand = new RelayCommand(() => StartViewModel<AddGroupViewModel>(new Pipe(true)));
            ShowChildDetailsCommand = new RelayCommand(() => StartViewModel<ChildDetailsViewModel>(new Pipe(false)));

            NamesCaseSensitiveChildrenFilter = false;

            if (IsDesignerMode) return;

            Load();
        }

        #region Windows

        private IWindowService _addGroupView;
        private IWindowService AddGroupView => _addGroupView ?? (_addGroupView = WindowServices.AdditionGroupWindow);
        private IWindowService _addChildView;
        private IWindowService AddChildView => _addChildView ?? (_addChildView = WindowServices.AdditionChildWindow);
        private IWindowService _childDetailsView;
        private IWindowService ChildDetailsView => _childDetailsView ?? (_childDetailsView = WindowServices.ChildDetailsWindow);

        #endregion

        private async void Load()
        {
            DateTime from, to;
            from = to = DateTime.Now;
            var c = await Task.Run(() =>
            {
                var result = _context
                    .Children
                    .Include("Person")
                    .Include("Group")
                    .Include("ParentsChildren.Parent")
                    .ToArray();
                if (result.Length > 0)
                {
                    from = result.Min(t => t.EnterDate);
                    to = result.Max(t => t.EnterDate);
                }
                return result;
            });
            Children = CollectionViewSource.GetDefaultView(c);
            FromEnterDateChildrenFilter = from;
            TillEnterDateChildrenFilter = to;
            Groups = await Task.Run(() => new ObservableCollection<Group>(_context.Groups));
        }

        private async void UpdateChild()
        {
            UpdateChildCommand.NotifyCanExecute(false);
            await Task.Run(() => _context.SaveChanges());
            UpdateChildCommand.NotifyCanExecute(true);
        }

        private void SetPersonFullName()
        {
            var lasts = PersonLastNameChildrenFilter.Split(' ');
            var firsts = PersonFirstNameChildrenFilter.Split(' ');
            var pats = PersonPatronymicChildrenFilter.Split(' ');

            int len = Math.Max(Math.Max(lasts.Length, firsts.Length), pats.Length);
            string[] words = new string[3 * len];
            for (int i = 0; i < len; i++)
            {
                words[i * 3] = i < lasts.Length ? lasts[i] : "";
                words[i * 3 + 1] = i < firsts.Length ? firsts[i] : "";
                words[i * 3 + 2] = i < pats.Length ? pats[i] : "";
            }
            // don't call set
            _personFullNameChildrenFilter = string.Join(" ", words);
            OnPropertyChangedRefreshFilter(nameof(PersonFullNameChildrenFilter));
        }

        private static string[] SplitStringForFilter(string s)
        {
            return s.Replace('ё', 'е').Replace('Ё', 'Е').Split(FullNameSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool ChildFilter(object o)
        {
            var c = (Child)o;
            var p = c.Person;


            // Archive
            if (DataFromArchiveChildrenFilter.HasValue)
            {
                const Groups f = DAL.Model.Groups.Finished;
                if (((c.Group.GroupType & f) == f) != DataFromArchiveChildrenFilter.Value)
                    return false;
            }

            // EnterDate
            if (FromEnterDateChildrenFilter > c.EnterDate || TillEnterDateChildrenFilter < c.EnterDate)
                return false;

            if (OnlyDebtors)
            {

            }

            // LastName, FirstName, Patronymic
            if (!string.IsNullOrEmpty(PersonFullNameChildrenFilter))
            {
                Func<string[], string, bool> eq = (array, s) =>
                {
                    if (array.Length == 0)
                        return true;

                    s = s
                        .Replace('ё', 'е')
                        .Replace('Ё', 'Е');
                    return WholeNamesChildrenFilter
                        ? array.Contains(s, NamesCaseSensitiveChildrenFilter ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
                        : array.Any(i => s.IndexOf(i, NamesCaseSensitiveChildrenFilter ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) >= 0);
                };

                if (
                    !(eq(_lastNameChildrenFilterStrings, p.LastName) &&
                      eq(_firstNameChildrenFilterStrings, p.FirstName) &&
                      eq(_patronymicChildrenFilterStrings, p.Patronymic)))
                    return false;

            }
            return true;
        }

        private void OnPropertyChangedRefreshFilter([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
            Children.TryRefreshFilter();
        }

        #region Search

        private string _personFirstNameChildrenFilter = string.Empty;
        public string PersonFirstNameChildrenFilter
        {
            get { return _personFirstNameChildrenFilter; }
            set
            {
                if (value == _personFirstNameChildrenFilter) return;
                _personFirstNameChildrenFilter = value;
                _firstNameChildrenFilterStrings = SplitStringForFilter(PersonFirstNameChildrenFilter);
                OnPropertyChanged();
                SetPersonFullName();
            }
        }

        private string _personLastNameChildrenFilter = string.Empty;
        public string PersonLastNameChildrenFilter
        {
            get { return _personLastNameChildrenFilter; }
            set
            {
                if (value == _personLastNameChildrenFilter) return;
                _personLastNameChildrenFilter = value;
                _lastNameChildrenFilterStrings = SplitStringForFilter(PersonLastNameChildrenFilter);
                OnPropertyChanged();
                SetPersonFullName();
            }
        }

        private string _personPatronymicChildrenFilter = string.Empty;
        public string PersonPatronymicChildrenFilter
        {
            get { return _personPatronymicChildrenFilter; }
            set
            {
                if (value == _personPatronymicChildrenFilter) return;
                _personPatronymicChildrenFilter = value;
                _patronymicChildrenFilterStrings = SplitStringForFilter(PersonPatronymicChildrenFilter);
                OnPropertyChanged();
                SetPersonFullName();
            }
        }
        
        private string _personFullNameChildrenFilter = string.Empty;
        public string PersonFullNameChildrenFilter
        {
            get { return _personFullNameChildrenFilter; }
            set
            {
                if (value == _personFullNameChildrenFilter) return;
                _personFullNameChildrenFilter = value;

                var names = value.Split(' ');

                int len0 = names.Length / 3;
                int len1 = len0+1;

                var last = new string[names.Length % 3 == 0 ? len0 : len1];
                var first = new string[names.Length % 3 <= 1 ? len0 : len1];
                var pat = new string[len0];
                for (int i = 0, j = 0; i < len0; i++, j += 3)
                {
                    last[i] = names[j];
                    first[i] = names[j + 1];
                    pat[i] = names[j + 2];
                }

                switch (names.Length % 3)
                {
                    case 0:
                        break;
                    case 1:
                        last[last.Length-1] = names[names.Length - 1];
                        break;
                    case 2:
                        last[last.Length -1] = names[names.Length - 2];
                        first[last.Length -1] = names[names.Length - 1];
                        break;
                }


                _personLastNameChildrenFilter = string.Join(" ", last);
                _personFirstNameChildrenFilter = string.Join(" ", first);
                _personPatronymicChildrenFilter = string.Join(" ", pat);

                _lastNameChildrenFilterStrings = last;
                _firstNameChildrenFilterStrings = first;
                _patronymicChildrenFilterStrings = pat;

                OnPropertyChanged(nameof(PersonLastNameChildrenFilter));
                OnPropertyChanged(nameof(PersonFirstNameChildrenFilter));
                OnPropertyChanged(nameof(PersonPatronymicChildrenFilter));
                OnPropertyChangedRefreshFilter();
            }
        }

        public DateTime? FromEnterDateChildrenFilter
        {
            get { return _fromEnterDateChildrenFilter; }
            set
            {
                if (value.Equals(_fromEnterDateChildrenFilter)) return;
                _fromEnterDateChildrenFilter = value;
                OnPropertyChangedRefreshFilter();
            }
        }

        public DateTime? TillEnterDateChildrenFilter
        {
            get { return _tillEnterDateChildrenFilter; }
            set
            {
                if (value.Equals(_tillEnterDateChildrenFilter)) return;
                _tillEnterDateChildrenFilter = value;
                OnPropertyChangedRefreshFilter();
            }
        }

        public bool WholeNamesChildrenFilter
        {
            get { return _wholeNamesChildrenFilter; }
            set
            {
                if (value == _wholeNamesChildrenFilter) return;
                _wholeNamesChildrenFilter = value;
                OnPropertyChangedRefreshFilter();
            }
        }

        public bool NamesCaseSensitiveChildrenFilter
        {
            get { return _namesCaseSensitiveChildrenFilter; }
            set
            {
                if (value == _namesCaseSensitiveChildrenFilter) return;
                _namesCaseSensitiveChildrenFilter = value;
                OnPropertyChangedRefreshFilter();
            }
        }

        public bool? DataFromArchiveChildrenFilter
        {
            get { return _dataFromArchiveChildrenFilter; }
            set
            {
                if (value == _dataFromArchiveChildrenFilter) return;
                _dataFromArchiveChildrenFilter = value;
                OnPropertyChangedRefreshFilter();
            }
        }

        public bool OnlyDebtors
        {
            get { return _onlyDebtors; }
            set
            {
                if (value == _onlyDebtors) return;
                _onlyDebtors = value;
                OnPropertyChangedRefreshFilter();
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

        //BindingListCollectionView
        private ICollectionView _children;
        private DateTime? _fromEnterDateChildrenFilter;
        private DateTime? _tillEnterDateChildrenFilter;

        public ICollectionView Children
        {
            get { return _children; }
            private set
            {
                if (Equals(value, _children)) return;
                _children = value;
                _children.Filter = ChildFilter;
                OnPropertyChanged();
            }
        }


        public IRelayCommand UpdateChildCommand { get; }
        public IRelayCommand ShowAddGroupWindowCommand { get; }
        public IRelayCommand ShowAddChildWindowCommand { get; }
        public IRelayCommand ShowChildDetailsCommand { get; }

        private static readonly char[] FullNameSeparators = { ' ' };
        private string[] _firstNameChildrenFilterStrings = new string[0];
        private string[] _lastNameChildrenFilterStrings = new string[0];
        private string[] _patronymicChildrenFilterStrings = new string[0];
        private bool _wholeNamesChildrenFilter;
        private bool _namesCaseSensitiveChildrenFilter;
        private bool? _dataFromArchiveChildrenFilter;
        private bool _onlyDebtors;
    }
}