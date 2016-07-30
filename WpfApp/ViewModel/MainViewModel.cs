using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.Util;

// ReSharper disable ExplicitCallerInfoArgument

namespace WpfApp.ViewModel
{
    internal class MainViewModel : ViewModelBase
    {
        private KindergartenContext _context;
        public MainViewModel()
        {
            ShowAddChildWindowCommand = new RelayCommand(() => StartViewModel<AddChildViewModel>(new Pipe(true)));
            ShowAddGroupWindowCommand = new RelayCommand(() => StartViewModel<AddGroupViewModel>(new Pipe(true)));
            ShowChildDetailsCommand = new RelayCommand(ShowChildDetails);
            RefreshDataCommand = new RelayCommand(Load);
            AddNewTarifCommand = new RelayCommand<Tarif>(AddNewTarif);

            NamesCaseSensitiveChildrenFilter = false;

            if (IsDesignerMode) return;

            Load();
        }

        private void AddNewTarif(Tarif tarif)
        {
            Console.WriteLine(tarif.IsValid());
            if (!tarif.IsValid()) return;

//            _context.Tarifs.Add(tarif);
//            _context.SaveChanges();
//            Tarifs.Add(tarif);
        }

        private void ShowChildDetails()
        {
            var child = SelectedChild;
            if (child == null) return;

            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["child"] = child,
                ["groups"] = Groups,
                ["owner"] = this,
                ["context"] = _context,
                ["tarifs"] = Tarifs,
            };
            StartViewModel<ChildDetailsViewModel>(new Pipe(parameters, false));
        }

        private async void Load()
        {
            RefreshDataCommand.NotifyCanExecute(false);
            ++LoadingDataCount;

            _context = new KindergartenContext();
            await UpdateChildrenAsync();
            await UpdateGroupsAsync();
            await UpdateTarifsAsync();

            --LoadingDataCount;
            RefreshDataCommand.NotifyCanExecute(true);
        }

        private static readonly Random Random = new Random();

        public async Task UpdateChildrenAsync()
        {
            ++LoadingDataCount;

            DateTime from, to;
            from = to = DateTime.Now;
            int childNoArchivedCount = 0;

            var c = await Task.Run(() =>
            {
                var result = _context
                    .Children
                    .Include("Person")
                    .Include("Group")
                    .Include("Tarif")
                    .Include("ParentsChildren.Parent")
                    .ToList();
//                Thread.Sleep(Random.Next(1000));

                if (result.Count > 0)
                {
                    if (!FromEnterDateChildrenFilter.HasValue) from = result.Min(t => t.EnterDate);
                    if (!TillEnterDateChildrenFilter.HasValue) to = result.Max(t => t.EnterDate);
                    childNoArchivedCount = result.Count(t => (t.Options & ChildOptions.Archived) == 0);
                }
                return result;
            });
            if (!FromEnterDateChildrenFilter.HasValue) FromEnterDateChildrenFilter = from;
            if (!TillEnterDateChildrenFilter.HasValue) TillEnterDateChildrenFilter = to;

            var selectedChild = SelectedChild;
            Children = (ListCollectionView) CollectionViewSource.GetDefaultView(c);
            if (selectedChild != null && c.Count > 0)
                SelectedChild = c.FirstOrDefault(ch => ch.Id == selectedChild.Id);

            ChildNoArchivedCount = childNoArchivedCount;
            ChildTotalCount = c.Count;

            --LoadingDataCount;
        }

        private async Task UpdateGroupsAsync()
        {
            ++LoadingDataCount;
            Groups = await Task.Run(() => new ObservableCollection<Group>(_context.Groups));
            --LoadingDataCount;
        }

        public async Task UpdateTarifsAsync()
        {
            ++LoadingDataCount;
            Tarifs = await Task.Run(() =>
            {
                var list = _context.Tarifs.ToList();
                return new ObservableCollection<Tarif>(list);
            });
            --LoadingDataCount;
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
            OnPropertyChangedAndRefreshChildrenFilter(nameof(PersonFullNameChildrenFilter));
        }

        private static string[] SplitStringForFilter(string s)
        {
            return s.Replace('ё', 'е').Replace('Ё', 'Е').Split(FullNameSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool ChildFilter(object o)
        {
            var c = (Child)o;
            var p = c.Person;


            // Archived
            if (ArchivedChildrenFilter.HasValue)
            {
                if (ArchivedChildrenFilter.Value ^ ((c.Options & ChildOptions.Archived) == ChildOptions.Archived))
                    return false;
            }

            // EnterDate
            if (FromEnterDateChildrenFilter > c.EnterDate || TillEnterDateChildrenFilter < c.EnterDate)
                return false;

            if (OnlyDebtorsChildrenFilter)
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

        private void OnPropertyChangedAndRefreshChildrenFilter([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
            Children.TryRefreshFilter();
        }


        #region Search

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
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public DateTime? FromEnterDateChildrenFilter
        {
            get { return _fromEnterDateChildrenFilter; }
            set
            {
                if (value.Equals(_fromEnterDateChildrenFilter)) return;
                _fromEnterDateChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public DateTime? TillEnterDateChildrenFilter
        {
            get { return _tillEnterDateChildrenFilter; }
            set
            {
                if (value.Equals(_tillEnterDateChildrenFilter)) return;
                _tillEnterDateChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public bool WholeNamesChildrenFilter
        {
            get { return _wholeNamesChildrenFilter; }
            set
            {
                if (value == _wholeNamesChildrenFilter) return;
                _wholeNamesChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public bool NamesCaseSensitiveChildrenFilter
        {
            get { return _namesCaseSensitiveChildrenFilter; }
            set
            {
                if (value == _namesCaseSensitiveChildrenFilter) return;
                _namesCaseSensitiveChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public bool? ArchivedChildrenFilter
        {
            get { return _archivedChildrenFilter; }
            set
            {
                if (value == _archivedChildrenFilter) return;
                _archivedChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public bool OnlyDebtorsChildrenFilter
        {
            get { return _onlyDebtorsChildrenFilter; }
            set
            {
                if (value == _onlyDebtorsChildrenFilter) return;
                _onlyDebtorsChildrenFilter = value;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        #endregion

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
        public ListCollectionView Children
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

        public int ChildNoArchivedCount
        {
            get { return _childNoArchivedCount; }
            set
            {
                if (value == _childNoArchivedCount) return;
                _childNoArchivedCount = value;
                OnPropertyChanged();
            }
        }
        public int ChildTotalCount
        {
            get { return _childTotalCount; }
            set
            {
                if (value == _childTotalCount) return;
                _childTotalCount = value;
                OnPropertyChanged();
            }
        }

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
        public Child SelectedChild
        {
            get { return _selectedChild; }
            set
            {
                if (Equals(value, _selectedChild)) return;
                _selectedChild = value;
                OnPropertyChanged();
            }
        }
        public bool IsDataLoading
        {
            get { return _isDataLoading; }
            set
            {
                if (value == _isDataLoading) return;
                _isDataLoading = value;
                OnPropertyChanged();
            }
        }

        
        public IRelayCommand ShowAddGroupWindowCommand { get; }
        public IRelayCommand ShowAddChildWindowCommand { get; }
        public IRelayCommand ShowChildDetailsCommand { get; }
        public IRelayCommand RefreshDataCommand { get; }
        public IRelayCommand AddNewTarifCommand { get; }

        private int LoadingDataCount
        {
            get { return _loadingDataCount; }
            set
            {
                _loadingDataCount = value;
                IsDataLoading = value != 0;
            }
        }

        #region Fields

        private static readonly char[] FullNameSeparators = { ' ' };
        private string[] _firstNameChildrenFilterStrings = new string[0];
        private string[] _lastNameChildrenFilterStrings = new string[0];
        private string[] _patronymicChildrenFilterStrings = new string[0];
        private string _personFirstNameChildrenFilter = string.Empty;
        private string _personLastNameChildrenFilter = string.Empty;
        private string _personPatronymicChildrenFilter = string.Empty;
        private string _personFullNameChildrenFilter = string.Empty;
        private bool _wholeNamesChildrenFilter;
        private bool _namesCaseSensitiveChildrenFilter;
        private bool? _archivedChildrenFilter;
        private bool _onlyDebtorsChildrenFilter;
        private string _title;
        private ListCollectionView _children;
        private ObservableCollection<Group> _groups;
        private Child _selectedChild;
        private DateTime? _fromEnterDateChildrenFilter;
        private DateTime? _tillEnterDateChildrenFilter;
        private bool _isDataLoading;
        private int _loadingDataCount;
        private int _childTotalCount;
        private int _childNoArchivedCount;
        private ObservableCollection<Tarif> _tarifs;

        #endregion
    }
}