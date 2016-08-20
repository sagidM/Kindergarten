using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.View.DialogService;

namespace WpfApp.ViewModel
{
    public class ChildDetailsViewModel : ViewModelBase
    {
        public IRelayCommand SaveChangesCommand { get; }
        public IRelayCommand ChangeGroupCommand { get; }
        public IRelayCommand AddChildToArchiveCommand { get; }
        public IRelayCommand RemoveChildFromArchiveCommand { get; }
        public IRelayCommand AttachParentCommand { get; }
        public IRelayCommand DetachParentCommand { get; }

        public ChildDetailsViewModel()
        {
            SaveChangesCommand = new RelayCommand(SaveChanges);
            ChangeGroupCommand = new RelayCommand(ChangeGroup);
            AddChildToArchiveCommand = new RelayCommand<string>(AddChildToArchive);
            RemoveChildFromArchiveCommand = new RelayCommand(RemoveChildFromArchive);
            AttachParentCommand = new RelayCommand<Parents>(AttachParent);
            DetachParentCommand = new RelayCommand<Parents>(DetachParent);

            _childNotifier = new DirtyPropertyChangeNotifier();
            _childNotifier.StartTracking();
            _fatherNotifier = new DirtyPropertyChangeNotifier();
            _fatherNotifier.StartTracking();
            _motherNotifier = new DirtyPropertyChangeNotifier();
            _motherNotifier.StartTracking();
            _otherNotifier = new DirtyPropertyChangeNotifier();
            _otherNotifier.StartTracking();

            Action onToggleDirty = () => OnPropertyChanged(nameof(IsDirty));
            _childNotifier.ToggleDirty += onToggleDirty;
            _fatherNotifier.ToggleDirty += onToggleDirty;
            _motherNotifier.ToggleDirty += onToggleDirty;
            _otherNotifier.ToggleDirty += onToggleDirty;
        }

        public override async void OnLoaded()
        {
            int id = (int) Pipe.GetParameter("child_id");

            Child currentChild = null;
            Parent currentFather = null, currentMother = null, currentOther = null;
            IEnumerable<Group> groups = null;
            IEnumerable<Tarif> tarifs = null;
            MainViewModel mainViewModel = null;

            await Task.Run(() =>
            {
                _childContext = new KindergartenContext();
                var enters = _childContext.EnterChildren.Include("Child.Person").Include("Child.Group").Where(e => e.ChildId == id).ToList();
                var context = new KindergartenContext();
                currentFather = context.ParentChildren
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Father)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _fatherContext = currentFather != null ? context : null;

                context = new KindergartenContext();
                currentMother = context.ParentChildren.Include("Parent.Person")
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Mother)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _motherContext = currentMother != null ? context : null;

                context = new KindergartenContext();
                currentOther = context.ParentChildren.Include("Parent.Person")
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Other)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _otherContext = currentOther != null ? context : null;

                int maxEnterIndex = -1;
                DateTime maxDateTime = DateTime.MinValue;
                for (int i = 0; i < enters.Count; i++)
                {
                    if (maxDateTime < enters[i].EnterDate)
                    {
                        maxDateTime = enters[i].EnterDate;
                        maxEnterIndex = i;
                    }
                }
                if (maxEnterIndex == -1) throw new NotImplementedException();
                currentChild = enters[0].Child; // 0 - enters consists of same elements
                currentChild.LastEnterChild = enters[maxEnterIndex];

                groups = (IEnumerable<Group>) Pipe.GetParameter("groups");
                tarifs = (IEnumerable<Tarif>) Pipe.GetParameter("tarifs");
                mainViewModel = (MainViewModel) Pipe.GetParameter("owner");
            });

            CurrentChild = currentChild;
            CurrentFather = currentFather;
            CurrentMother = currentMother;
            CurrentOther = currentOther;
            Groups = groups;
            Tarifs = tarifs;
            _mainViewModel = mainViewModel;

            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentGroup));
            OnPropertyChanged(nameof(CurrentChildTarif));

            _childNotifier.SetProperty(nameof(CurrentChildTarif), CurrentChildTarif);
            _childNotifier.SetProperty("FatherId", CurrentFather?.Id);
            _childNotifier.SetProperty("MotherId", CurrentMother?.Id);
            _childNotifier.SetProperty("OtherId", CurrentOther?.Id);

            OnPropertyChanged(nameof(CurrentChild));
        }

        private void DetachParent(Parents parentType)
        {
            DetachParentCommand.NotifyCanExecute(false);
            KindergartenContext context;
            DirtyPropertyChangeNotifier notifier;
            switch (parentType)
            {
                case Parents.Father:
                    notifier = _fatherNotifier;
                    context = _fatherContext;
                    break;
                case Parents.Mother:
                    notifier = _motherNotifier;
                    context = _motherContext;
                    break;
                case Parents.Other:
                    notifier = _otherNotifier;
                    context = _otherContext;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parentType), parentType, null);
            }
            if (notifier.HasDirty)
            {
                var savingPipe = new Pipe(true);
                savingPipe.SetParameter("parent_type", parentType);
                StartViewModel<SaveOrRepealParentChangesViewModel>(savingPipe);
                var savingResult = (SavingResult) savingPipe.GetParameter("saving_result");
                switch (savingResult)
                {
                    case SavingResult.Save:
                        context.SaveChanges();
                        break;
                    case SavingResult.NotSave:
                        break;
                    case SavingResult.Cancel:
                        DetachParentCommand.NotifyCanExecute(true);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            notifier.ClearDirties();

            switch (parentType)
            {
                case Parents.Father:
                    CurrentFather = null;
                    _fatherContext = null;
                    _childNotifier.OnPropertyChanged(null, "FatherId");
                    break;
                case Parents.Mother:
                    CurrentMother = null;
                    _motherContext = null;
                    _childNotifier.OnPropertyChanged(null, "MotherId");
                    break;
                case Parents.Other:
                    CurrentOther = null;
                    _otherContext = null;
                    _childNotifier.OnPropertyChanged(null, "OtherId");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parentType), parentType, null);
            }
            DetachParentCommand.NotifyCanExecute(true);
        }

        private void AttachParent(Parents parentType)
        {
            var pipe = new Pipe(true);

            string text = null;
            if (parentType == Parents.Other)
            {
                text = IODialog.InputDialog("Кем приходится ребёнку", "Иной представитель", OtherParentText);
                if (text == null)
                    return;
            }
            StartViewModel<AddParentViewModel>(pipe);
            var parent0 = (Parent) pipe.GetParameter("parent_result");
            if (parent0 == null) return;
            var context = new KindergartenContext();
            var parent = context.Parents.First(p => p.Id == parent0.Id);

            switch (parentType)
            {
                case Parents.Father:
                    CurrentFather = parent;
                    _fatherContext = context;
                    _childNotifier.OnPropertyChanged(CurrentFather.Id, "FatherId");
                    break;
                case Parents.Mother:
                    CurrentMother = parent;
                    _motherContext = context;
                    _childNotifier.OnPropertyChanged(CurrentMother.Id, "MotherId");
                    break;
                case Parents.Other:
                    OtherParentText = text;
                    CurrentOther = parent;
                    _otherContext = context;
                    _childNotifier.OnPropertyChanged(CurrentOther.Id, "OtherId");
                    break;
            }
        }

        private async void SaveChanges()
        {
            SaveChangesCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var parents = context.ParentChildren
                    .Where(pc => pc.ChildId == CurrentChild.Id)
                    .ToList();

                var father = parents.FirstOrDefault(p => p.ParentType == Parents.Father);
                var mother = parents.FirstOrDefault(p => p.ParentType == Parents.Mother);
                var other = parents.FirstOrDefault(p => p.ParentType == Parents.Other);

                Action<ParentChild, Parent, Parents> changeParent = (old, currentParent, type) =>
                {
                    if (old == null)
                    {
                        if (currentParent != null)
                        {
                            context.ParentChildren.Add(new ParentChild { ChildId = CurrentChild.Id, ParentId = currentParent.Id, ParentType = type });
                        }
                    }
                    else
                    {
                        if (currentParent == null)
                        {
                            context.ParentChildren.Remove(old);
                        }
                        else if (old.ParentId != currentParent.Id)
                        {
                            //old.ParentId = currentParent.Id;
                            context.ParentChildren.Remove(old);
                            context.ParentChildren.Add(new ParentChild {ChildId=CurrentChild.Id, ParentId = currentParent.Id, ParentType = type, ParentTypeText = old.ParentTypeText});
                            // TODO: Here must be right update query
                        }
                    }
                };
                changeParent(father, CurrentFather, Parents.Father);
                changeParent(mother, CurrentMother, Parents.Mother);
                changeParent(other, CurrentOther, Parents.Other);

                context.SaveChanges();
                _childContext.SaveChanges();
                _fatherContext?.SaveChanges();
                _motherContext?.SaveChanges();
                _otherContext?.SaveChanges();


                _childNotifier.ClearDirties();
                _fatherNotifier.ClearDirties();
                _motherNotifier.ClearDirties();
                _otherNotifier.ClearDirties();
            });
            await UpdateMainViewModel();
            SaveChangesCommand.NotifyCanExecute(true);
        }

        private void AddChildToArchive(string note)
        {
            if (CurrentChildIsArchived) throw new InvalidOperationException();

            var context = new KindergartenContext();
            var enter = context.EnterChildren.First(e => e.Id == CurrentChild.LastEnterChild.Id);
            enter.ExpulsionDate = DateTime.Now;
            enter.ExpulsionNote = note;
            context.SaveChanges();

            CurrentChild.LastEnterChild = enter;

            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentChild));
            OnPropertyChanged(nameof(ExpulsionDateLastEnterChild));
            OnPropertyChanged(nameof(ExpulsionNoteLastEnterChild));
        }

        private void RemoveChildFromArchive()
        {
            if (!CurrentChildIsArchived) throw new InvalidOperationException();
            var group = CurrentChild.Group;
            if ((group.GroupType & DAL.Model.Groups.Finished) != 0)
            {
                var boxResult = MessageBox.Show($"Группа \"{@group.Name}\" с номером {@group.Id} состоит в архиве\r\n" + "Хотите убрать группу и данного ребёнка из архива?", "Архив", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (boxResult == MessageBoxResult.Yes)
                    group.GroupType ^= DAL.Model.Groups.Finished;
                else
                    return;
            }

            var context = new KindergartenContext();
            context.EnterChildren.Add(CurrentChild.LastEnterChild = new EnterChild {EnterDate = DateTime.Now, ChildId = CurrentChild.Id});
            context.SaveChanges();
            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentChild));
            OnPropertyChanged(nameof(ExpulsionDateLastEnterChild));
            OnPropertyChanged(nameof(ExpulsionNoteLastEnterChild));
        }

        private async void ChangeGroup()
        {
            var parameters = new Dictionary<string, object>(3)
            {
                ["child"] = CurrentChild, ["groups"] = Groups,
            };
            var pipe = new Pipe(parameters, true);
            StartViewModel<ChangeChildGroupViewModel>(pipe);
            if (!(bool) pipe.GetParameter("saved_new_group"))
                return;

            var group = (Group) pipe.GetParameter("group_result");
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

        public bool IsDirty => _childNotifier.HasDirty || _fatherNotifier.HasDirty || _motherNotifier.HasDirty || _otherNotifier.HasDirty;

        #region CurrentChild (dirty)

        public Child CurrentChild
        {
            get { return _currentChild; }
            set
            {
                if (Equals(value, _currentChild)) return;
                _currentChild = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExpulsionDateLastEnterChild));
                OnPropertyChanged(nameof(ExpulsionNoteLastEnterChild));

                OnPropertyChanged(nameof(CurrentChildPersonLastName));
                _childNotifier.SetProperty(nameof(CurrentChildPersonLastName), CurrentChildPersonLastName);
                OnPropertyChanged(nameof(CurrentChildPersonFirstName));
                _childNotifier.SetProperty(nameof(CurrentChildPersonFirstName), CurrentChildPersonFirstName);
                OnPropertyChanged(nameof(CurrentChildPersonPatronymic));
                _childNotifier.SetProperty(nameof(CurrentChildPersonPatronymic), CurrentChildPersonPatronymic);
                OnPropertyChanged(nameof(CurrentChildLocationAddress));
                _childNotifier.SetProperty(nameof(CurrentChildLocationAddress), CurrentChildLocationAddress);
                OnPropertyChanged(nameof(CurrentChildBirthDate));
                _childNotifier.SetProperty(nameof(CurrentChildBirthDate), CurrentChildBirthDate);
                OnPropertyChanged(nameof(CurrentChildIsNobody));
                _childNotifier.SetProperty(nameof(CurrentChildIsNobody), CurrentChildIsNobody);
                OnPropertyChanged(nameof(CurrentChildSex));
                _childNotifier.SetProperty(nameof(CurrentChildSex), CurrentChildSex);
            }
        }

        public string CurrentChildPersonLastName
        {
            get { return CurrentChild.Person.LastName; }
            set
            {
                var person = CurrentChild.Person;
                if (value == person.LastName) return;
                person.LastName = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentChildPersonFirstName
        {
            get { return CurrentChild.Person.FirstName; }
            set
            {
                var person = CurrentChild.Person;
                if (value == person.FirstName) return;
                person.FirstName = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentChildPersonPatronymic
        {
            get { return CurrentChild.Person.Patronymic; }
            set
            {
                var person = CurrentChild.Person;
                if (value == person.Patronymic) return;
                person.Patronymic = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentChildLocationAddress
        {
            get { return CurrentChild.LocationAddress; }
            set
            {
                if (value == CurrentChild.LocationAddress) return;
                CurrentChild.LocationAddress = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentChildBirthDate
        {
            get { return CurrentChild.BirthDate; }
            set
            {
                if (value == CurrentChild.BirthDate) return;
                CurrentChild.BirthDate = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public bool CurrentChildIsNobody
        {
            get { return CurrentChild.IsNobody; }
            set
            {
                if (CurrentChild.IsNobody == value) return;
                CurrentChild.IsNobody = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public Sex CurrentChildSex
        {
            get { return CurrentChild.Sex; }
            set
            {
                if (CurrentChild.Sex == value) return;
                CurrentChild.Sex = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public Tarif CurrentChildTarif
        {
            get { return Tarifs.First(t => t.Id == CurrentChild.TarifId); }
            set
            {
                if (CurrentChild.TarifId == value.Id) return;
                CurrentChild.TarifId = value.Id;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        #endregion

        #region CurrentFather (dirty)

        public Parent CurrentFather
        {
            get { return _currentFather; }
            set
            {
                if (Equals(value, _currentFather)) return;
                _currentFather = value;
                OnPropertyChanged();
                if (value == null) return;

                OnPropertyChanged(nameof(CurrentFatherPersonLastName));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPersonLastName), CurrentFatherPersonLastName);
                OnPropertyChanged(nameof(CurrentFatherPersonFirstName));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPersonFirstName), CurrentFatherPersonFirstName);
                OnPropertyChanged(nameof(CurrentFatherPersonPatronymic));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPersonPatronymic), CurrentFatherPersonPatronymic);
                OnPropertyChanged(nameof(CurrentFatherLocationAddress));
                _fatherNotifier.SetProperty(nameof(CurrentFatherLocationAddress), CurrentFatherLocationAddress);
                OnPropertyChanged(nameof(CurrentFatherResidenceAddress));
                _fatherNotifier.SetProperty(nameof(CurrentFatherResidenceAddress), CurrentFatherResidenceAddress);
                OnPropertyChanged(nameof(CurrentFatherWorkAddress));
                _fatherNotifier.SetProperty(nameof(CurrentFatherWorkAddress), CurrentFatherWorkAddress);
                OnPropertyChanged(nameof(CurrentFatherPassportIssueDate));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPassportIssueDate), CurrentFatherPassportIssueDate);
                OnPropertyChanged(nameof(CurrentFatherPassportIssuedBy));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPassportIssuedBy), CurrentFatherPassportIssuedBy);
                OnPropertyChanged(nameof(CurrentFatherPassportSeries));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPassportSeries), CurrentFatherPassportSeries);
                OnPropertyChanged(nameof(CurrentFatherPhoneNumber));
                _fatherNotifier.SetProperty(nameof(CurrentFatherPhoneNumber), CurrentFatherPhoneNumber);
            }
        }

        public string CurrentFatherPersonLastName
        {
            get { return CurrentFather.Person.LastName; }
            set
            {
                var person = CurrentFather.Person;
                if (person.LastName == value) return;
                person.LastName = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPersonFirstName
        {
            get { return CurrentFather.Person.FirstName; }
            set
            {
                var person = CurrentFather.Person;
                if (person.FirstName == value) return;
                person.FirstName = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPersonPatronymic
        {
            get { return CurrentFather.Person.Patronymic; }
            set
            {
                var person = CurrentFather.Person;
                if (person.Patronymic == value) return;
                person.Patronymic = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherLocationAddress
        {
            get { return CurrentFather.LocationAddress; }
            set
            {
                if (CurrentFather.LocationAddress == value) return;
                CurrentFather.LocationAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherResidenceAddress
        {
            get { return CurrentFather.ResidenceAddress; }
            set
            {
                if (CurrentFather.ResidenceAddress == value) return;
                CurrentFather.ResidenceAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherWorkAddress
        {
            get { return CurrentFather.WorkAddress; }
            set
            {
                if (CurrentFather.WorkAddress == value) return;
                CurrentFather.WorkAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPhoneNumber
        {
            get { return CurrentFather.PhoneNumber; }
            set
            {
                if (CurrentFather.PhoneNumber == value) return;
                CurrentFather.PhoneNumber = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPassportSeries
        {
            get { return CurrentFather.PassportSeries; }
            set
            {
                if (CurrentFather.PassportSeries == value) return;
                CurrentFather.PassportSeries = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPassportIssuedBy
        {
            get { return CurrentFather.PassportIssuedBy; }
            set
            {
                if (CurrentFather.PassportIssuedBy == value) return;
                CurrentFather.PassportIssuedBy = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentFatherPassportIssueDate
        {
            get { return CurrentFather.PassportIssueDate; }
            set
            {
                if (CurrentFather.PassportIssueDate == value) return;
                CurrentFather.PassportIssueDate = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        #endregion

        #region CurrentMother (dirty)

        public Parent CurrentMother
        {
            get { return _currentMother; }
            set
            {
                if (Equals(value, _currentMother)) return;
                _currentMother = value;
                OnPropertyChanged();
                if (value == null) return;

                OnPropertyChanged(nameof(CurrentMotherPersonLastName));
                _motherNotifier.SetProperty(nameof(CurrentMotherPersonLastName), CurrentMotherPersonLastName);
                OnPropertyChanged(nameof(CurrentMotherPersonFirstName));
                _motherNotifier.SetProperty(nameof(CurrentMotherPersonFirstName), CurrentMotherPersonFirstName);
                OnPropertyChanged(nameof(CurrentMotherPersonPatronymic));
                _motherNotifier.SetProperty(nameof(CurrentMotherPersonPatronymic), CurrentMotherPersonPatronymic);
                OnPropertyChanged(nameof(CurrentMotherLocationAddress));
                _motherNotifier.SetProperty(nameof(CurrentMotherLocationAddress), CurrentMotherLocationAddress);
                OnPropertyChanged(nameof(CurrentMotherResidenceAddress));
                _motherNotifier.SetProperty(nameof(CurrentMotherResidenceAddress), CurrentMotherResidenceAddress);
                OnPropertyChanged(nameof(CurrentMotherWorkAddress));
                _motherNotifier.SetProperty(nameof(CurrentMotherWorkAddress), CurrentMotherWorkAddress);
                OnPropertyChanged(nameof(CurrentMotherPassportIssueDate));
                _motherNotifier.SetProperty(nameof(CurrentMotherPassportIssueDate), CurrentMotherPassportIssueDate);
                OnPropertyChanged(nameof(CurrentMotherPassportIssuedBy));
                _motherNotifier.SetProperty(nameof(CurrentMotherPassportIssuedBy), CurrentMotherPassportIssuedBy);
                OnPropertyChanged(nameof(CurrentMotherPassportSeries));
                _motherNotifier.SetProperty(nameof(CurrentMotherPassportSeries), CurrentMotherPassportSeries);
                OnPropertyChanged(nameof(CurrentMotherPhoneNumber));
                _motherNotifier.SetProperty(nameof(CurrentMotherPhoneNumber), CurrentMotherPhoneNumber);
            }
        }

        public string CurrentMotherPersonLastName
        {
            get { return CurrentMother.Person.LastName;
            }
            set
            {
                var person = CurrentMother.Person;
                if (person.LastName == value) return;
                person.LastName = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPersonFirstName
        {
            get { return CurrentMother.Person.FirstName; }
            set
            {
                var person = CurrentMother.Person;
                if (person.FirstName == value) return;
                person.FirstName = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPersonPatronymic
        {
            get { return CurrentMother.Person.Patronymic; }
            set
            {
                var person = CurrentMother.Person;
                if (person.Patronymic == value) return;
                person.Patronymic = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherLocationAddress
        {
            get { return CurrentMother.LocationAddress; }
            set
            {
                if (CurrentMother.LocationAddress == value) return;
                CurrentMother.LocationAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherResidenceAddress
        {
            get { return CurrentMother.ResidenceAddress; }
            set
            {
                if (CurrentMother.ResidenceAddress == value) return;
                CurrentMother.ResidenceAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherWorkAddress
        {
            get { return CurrentMother.WorkAddress; }
            set
            {
                if (CurrentMother.WorkAddress == value) return;
                CurrentMother.WorkAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPhoneNumber
        {
            get { return CurrentMother.PhoneNumber; }
            set
            {
                if (CurrentMother.PhoneNumber == value) return;
                CurrentMother.PhoneNumber = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPassportSeries
        {
            get { return CurrentMother.PassportSeries; }
            set
            {
                if (CurrentMother.PassportSeries == value) return;
                CurrentMother.PassportSeries = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPassportIssuedBy
        {
            get { return CurrentMother.PassportIssuedBy; }
            set
            {
                if (CurrentMother.PassportIssuedBy == value) return;
                CurrentMother.PassportIssuedBy = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentMotherPassportIssueDate
        {
            get { return CurrentMother.PassportIssueDate; }
            set
            {
                if (CurrentMother.PassportIssueDate == value) return;
                CurrentMother.PassportIssueDate = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        #endregion

        #region CurrentOther (dirty)

        public Parent CurrentOther
        {
            get { return _currentOther; }
            set
            {
                if (Equals(value, _currentOther)) return;
                _currentOther = value;
                OnPropertyChanged();
                if (value == null) return;

                OnPropertyChanged(nameof(CurrentOtherPersonLastName));
                _otherNotifier.SetProperty(nameof(CurrentOtherPersonLastName), CurrentOtherPersonLastName);
                OnPropertyChanged(nameof(CurrentOtherPersonFirstName));
                _otherNotifier.SetProperty(nameof(CurrentOtherPersonFirstName), CurrentOtherPersonFirstName);
                OnPropertyChanged(nameof(CurrentOtherPersonPatronymic));
                _otherNotifier.SetProperty(nameof(CurrentOtherPersonPatronymic), CurrentOtherPersonPatronymic);
                OnPropertyChanged(nameof(CurrentOtherLocationAddress));
                _otherNotifier.SetProperty(nameof(CurrentOtherLocationAddress), CurrentOtherLocationAddress);
                OnPropertyChanged(nameof(CurrentOtherResidenceAddress));
                _otherNotifier.SetProperty(nameof(CurrentOtherResidenceAddress), CurrentOtherResidenceAddress);
                OnPropertyChanged(nameof(CurrentOtherWorkAddress));
                _otherNotifier.SetProperty(nameof(CurrentOtherWorkAddress), CurrentOtherWorkAddress);
                OnPropertyChanged(nameof(CurrentOtherPassportIssueDate));
                _otherNotifier.SetProperty(nameof(CurrentOtherPassportIssueDate), CurrentOtherPassportIssueDate);
                OnPropertyChanged(nameof(CurrentOtherPassportIssuedBy));
                _otherNotifier.SetProperty(nameof(CurrentOtherPassportIssuedBy), CurrentOtherPassportIssuedBy);
                OnPropertyChanged(nameof(CurrentOtherPassportSeries));
                _otherNotifier.SetProperty(nameof(CurrentOtherPassportSeries), CurrentOtherPassportSeries);
                OnPropertyChanged(nameof(CurrentOtherPhoneNumber));
                _otherNotifier.SetProperty(nameof(CurrentOtherPhoneNumber), CurrentOtherPhoneNumber);
            }
        }

        public string CurrentOtherPersonLastName
        {
            get { return CurrentOther.Person.LastName; }
            set
            {
                var person = CurrentOther.Person;
                if (person.LastName == value) return;
                person.LastName = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPersonFirstName
        {
            get { return CurrentOther.Person.FirstName; }
            set
            {
                var person = CurrentOther.Person;
                if (person.FirstName == value) return;
                person.FirstName = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPersonPatronymic
        {
            get { return CurrentOther.Person.Patronymic; }
            set
            {
                var person = CurrentOther.Person;
                if (person.Patronymic == value) return;
                person.Patronymic = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherLocationAddress
        {
            get { return CurrentOther.LocationAddress; }
            set
            {
                if (CurrentOther.LocationAddress == value) return;
                CurrentOther.LocationAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherResidenceAddress
        {
            get { return CurrentOther.ResidenceAddress; }
            set
            {
                if (CurrentOther.ResidenceAddress == value) return;
                CurrentOther.ResidenceAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherWorkAddress
        {
            get { return CurrentOther.WorkAddress; }
            set
            {
                if (CurrentOther.WorkAddress == value) return;
                CurrentOther.WorkAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPhoneNumber
        {
            get { return CurrentOther.PhoneNumber; }
            set
            {
                if (CurrentOther.PhoneNumber == value) return;
                CurrentOther.PhoneNumber = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPassportSeries
        {
            get { return CurrentOther.PassportSeries; }
            set
            {
                if (CurrentOther.PassportSeries == value) return;
                CurrentOther.PassportSeries = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPassportIssuedBy
        {
            get { return CurrentOther.PassportIssuedBy; }
            set
            {
                if (CurrentOther.PassportIssuedBy == value) return;
                CurrentOther.PassportIssuedBy = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentOtherPassportIssueDate
        {
            get { return CurrentOther.PassportIssueDate; }
            set
            {
                if (CurrentOther.PassportIssueDate == value) return;
                CurrentOther.PassportIssueDate = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        #endregion

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

        public string OtherParentText
        {
            get { return _otherParentText; }
            private set
            {
                if (value == _otherParentText) return;
                _otherParentText = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ExpulsionDateLastEnterChild => CurrentChild.LastEnterChild.ExpulsionDate;
        public string ExpulsionNoteLastEnterChild => CurrentChild.LastEnterChild.ExpulsionNote;

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

        private Child _currentChild;
        private MainViewModel _mainViewModel;
        private IEnumerable<Group> _groups;
        private IEnumerable<Tarif> _tarifs;
        private KindergartenContext _childContext;
        private string _otherParentText;
        private readonly DirtyPropertyChangeNotifier _childNotifier;
        private readonly DirtyPropertyChangeNotifier _fatherNotifier;
        private readonly DirtyPropertyChangeNotifier _motherNotifier;
        private readonly DirtyPropertyChangeNotifier _otherNotifier;
        private Parent _currentFather;
        private Parent _currentMother;
        private Parent _currentOther;
        private KindergartenContext _fatherContext;
        private KindergartenContext _motherContext;
        private KindergartenContext _otherContext;
    }
}