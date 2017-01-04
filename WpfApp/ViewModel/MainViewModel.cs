using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using DAL.Model;
using Microsoft.Win32;
using WpfApp.Framework;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.Settings;
using WpfApp.Util;
using WpfApp.View.Converter;
using WpfApp.View.DialogService;
using static WpfApp.App;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable ExplicitCallerInfoArgument

namespace WpfApp.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            if (IsDesignerMode) return;

            var mainWindow = Application.Current.MainWindow;
            var saver = WindowStateSaver.ConfigureWindow(Application.Current.StartupUri.LocalPath, mainWindow, this);
            mainWindow.Closed += (w, e) => saver.Snapshot();

            ShowAddChildCommand = new RelayCommand(ShowAddChildWindow);
            ShowAddGroupCommand = new RelayCommand(ShowAddGroupWindow);
            ShowChildDetailsCommand = new RelayCommand<Child>(ShowChildDetails);
            ShowAddNewTarifCommand = new RelayCommand(ShowAddNewTarif);
            RefreshDataCommand = new RelayCommand(Load);
            DeleteSelectedTarifCommand = new RelayCommand(DeleteSelectedTarif);
            ChangeGroupGroupTypeCommand = new RelayCommand<Group>(ShowChangeGroupGroupType);
            SaveGroupCommand = new RelayCommand(SaveGroup);
            SaveTarifCommand = new RelayCommand<Tarif>(SaveTarif);
            GroupToggleArchiveCommand = new RelayCommand<Group>(GroupToggleArchive);
            ShowAddExpenseCommand = new RelayCommand(ShowAddExpense);
            RemoveExpenseCommand = new RelayCommand<Expense>(RemoveExpense);
            ResetExpensesFilterCommand = new RelayCommand(ResetExpensesFilter);
            AddIncomeCommand = new RelayCommand<FrameworkElement>(AddIncome);
            ResetIncomesFilterCommand = new RelayCommand(ResetIncomesFilter);
            RemoveSelectedIncomeCommand = new RelayCommand<IncomeDTO>(RemoveSelectedIncome);
            PrintDocumentChildrenCommand = new RelayCommand(PrintDocumentChildren);

            NamesCaseSensitiveChildrenFilter = false;

            Load();
            IsShowedOnlyIncomesFilter = (bool) this[nameof(IsShowedOnlyIncomesFilter), true];
            IsShowedMonthlyIncomesFilter = (bool) this[nameof(IsShowedMonthlyIncomesFilter), true];
            IsShowedAnnualIncomesFilter = (bool) this[nameof(IsShowedAnnualIncomesFilter), true];
            ShowGroupsFromArchive = (bool?) this[nameof(ShowGroupsFromArchive), null];
        }

        private void PrintDocumentChildren()
        {
            if (_childrenDocumentFileDialog == null)
            {
                _childrenDocumentFileDialog = new SaveFileDialog { FileName = AppFilePaths.GetNoticeFileName(), Filter = "Word|*.docx"};
            }
            if (_childrenDocumentFileDialog.ShowDialog() != true) return;

            string src = Path.GetFullPath(AppFilePaths.GetNoticeTemplatePath());
            string dest = _childrenDocumentFileDialog.FileName;

            var now = DateTime.Now;
            var data = Children.Cast<Child>()
                .Select(c => new Dictionary<string, string>
                {
                    ["&date_d"] = now.Day.ToString(),
                    ["&date_m"] = now.Month.ToString(),
                    ["&date_y"] = now.Year.ToString(),
                    ["&date_full"] = now.ToString(OtherSettings.DateFormat),
                    ["&child_id"] = c.Id.ToString(),
                    ["&child_first_name"] = c.Person.FirstName,
                    ["&child_second_name"] = c.Person.LastName,
                    ["&child_patronymic"] = c.Person.Patronymic,
                    ["&child_full_name"] = c.Person.FullName,
                    ["&child_location_address"] = c.LocationAddress,
                    ["&child_bithdate"] = c.BirthDate.ToString(OtherSettings.DateFormat),
                    ["&child_enter_date"] = c.LastEnterChild.EnterDate.ToString(OtherSettings.DateFormat),
                    ["&child_is_archived"] = c.LastEnterChild.ExpulsionDate.HasValue ? "да" : "нет",
                    ["&child_archived_status"] = c.LastEnterChild.ExpulsionDate.HasValue ? "в архиве" : "в саду",
                    ["&child_debt"] = c.MonthlyDebt.HasValue ? c.MonthlyDebt.Value.Str() : "нет оплат",
                    ["&child_sex"] = SexConverter.ConvertToString(c.Sex),
                    ["&group_id"] = c.GroupId.ToString(),
                    ["&group_name"] = c.Group.Name,
                    ["&tarif_id"] = c.TarifId.ToString(),
                    ["&tarif_note"] = c.Tarif.Note,
                    ["&tarif_monthly_payment"] = c.Tarif.MonthlyPayment.Str(),
                    ["&tarif_annual_payment"] = c.Tarif.AnnualPayment.Str(),
                })
                .ToArray();
            WordWorker.ReplaceWithDuplicate(src, dest, data);
        }

        private async void RemoveSelectedIncome(IncomeDTO income)
        {
            if (MessageBox.Show("Вы уверены, что удалить?", "Удаление", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            if (income == null || income.Prefix != IncomePrefix)
            {
                Logger.Warn("Trying to remove other income");
                return;
            }

            RemoveSelectedIncomeCommand.NotifyCanExecute(false);

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.Incomes.Remove(context.Incomes.First(i => i.Id == income.Id));
                context.SaveChanges();
            });
            await UpdateIncomesAsync();

            RemoveSelectedIncomeCommand.NotifyCanExecute(true);
        }

        private void ResetIncomesFilter()
        {
            SearchIncomesFilter = string.Empty;
            FromDateIncomesFilter = TillDateIncomesFilter = null;
            //IsShowedOnlyIncomesFilter = IsShowedMonthlyIncomesFilter = IsShowedAnnualIncomesFilter = true;
        }

        private async void AddIncome(FrameworkElement element)
        {
            var income = (Income) element.DataContext;
            element.DataContext = null;

            if (!income.IsValid()) return;

            AddIncomeCommand.NotifyCanExecute(false);

            var context = new KindergartenContext();
            income.IncomeDate = DateTime.Now;
            context.Incomes.Add(income.Clone());
            context.SaveChanges();

            await UpdateIncomesAsync();

            income.PersonName = string.Empty;
            income.Note = string.Empty;
            income.Money = 0;
            element.DataContext = income;

            AddIncomeCommand.NotifyCanExecute(true);
        }

        private void ResetExpensesFilter()
        {
            SearchExpensesFilter = string.Empty;
            FromDateExpensesFilter = _minFromDateExpenses;
            TillDateExpensesFilter = null;
            SelectedExpenseTypesExpensesFilter = null;
        }

        private async void RemoveExpense(Expense removingExpense)
        {
            if (MessageBox.Show("Вы уверены, что удалить?", "Удаление", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            RemoveExpenseCommand.NotifyCanExecute(false);

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var expense = context.Expenses.First(e => e.Id == removingExpense.Id);
                context.Expenses.Remove(expense);
                context.SaveChanges();
            });
            Expenses.Remove(removingExpense);

            RemoveExpenseCommand.NotifyCanExecute(true);
        }

        private async void ShowAddExpense()
        {
            var pipe = new Pipe(true);
            StartViewModel<AddExpenseViewModel>(pipe);
            var expense = (Expense)pipe.GetParameter("added_expense");
            if (expense == null) return;

            await UpdateExpensesAsync();
        }

        private void GroupToggleArchive(Group group)
        {
            Child[] children;
            int[] years;
            var context = new KindergartenContext();
            if ((group.GroupType & DAL.Model.Groups.Finished) != 0)
            {
                // finished -> non finished
                children = null;
                years = null;
            }
            else
            {
                // non finished -> finished
                var text = "Введите примечание для каждого ребёнка, добавляемого в архив";
                var extraInfo = "Внимание, добавление группы в архив добавит туда и всех детей находящихся в группе.\r\n" +
                                "Восстанавливать каждого ребёнка из архива придётся по отдельности.";
                var title = "Архив";
                var note = IODialog.InputDialog(text, title, $"Добавление группы \"{group.Name}\" в архив", extraInfo);
                if(note == null) return;

                var enters = context.EnterChildren
                    .Where(e => e.Child.Group.Id == group.Id && e.ExpulsionDate == null)
                    .Select(e => new {e, e.Child, e.Child.Person, e.Child.Group, e.Child.Tarif,
                        FirstEnterYear = context.EnterChildren.Where(e2 => e2.Child.Id == e.Child.Id).Min(e2 => e2.EnterDate).Year})
                    .ToArray();
                var now = DateTime.Now;
                children = new Child[enters.Length];
                years  = new int[enters.Length];
                for (int i = 0; i < enters.Length; i++)
                {
                    var res = enters[i];
                    res.e.ExpulsionDate = now;
                    res.e.ExpulsionNote = note;
                    var child = res.Child;
                    child.LastEnterChild = res.e;
                    child.Group = res.Group;
                    child.Tarif = res.Tarif;
                    child.Person = res.Person;
                    children[i] = child;
                    years[i] = res.FirstEnterYear;
                }
            }
            var groupEntity = context.Groups.First(g => g.Id == group.Id);
            groupEntity.GroupType ^= DAL.Model.Groups.Finished;
            group.GroupType ^= DAL.Model.Groups.Finished;
            context.SaveChanges();

            UpdateGroupsAsync().ConfigureAwait(false);
            if (children == null) return;
            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                Console.WriteLine("resource: {1}\\{0}", children[i].Id, years[i]);
                ChildDetailsViewModel.SaveDocumentAddingToArchive(child, child.Group, years[i]);
            }
        }

        private async void SaveTarif(Tarif tarif)
        {
            if (!tarif.IsValid()) return;

            var context = new KindergartenContext();
            var entity = context.Tarifs.First(t => t.Id == tarif.Id);
            entity.AnnualPayment = tarif.AnnualPayment;
            entity.MonthlyPayment = tarif.MonthlyPayment;
            entity.Note = tarif.Note;
            context.SaveChanges();
            await UpdateTarifsAsync();
        }

        private async void SaveGroup()
        {
            SaveGroupCommand.NotifyCanExecute(false);
            var s = SelectedGroup;
            var context = new KindergartenContext();
            var group = context.Groups.First(g => g.Id == s.Id);
            group.CreatedDate = s.CreatedDate;
            group.GroupType = s.GroupType;
            group.Name = s.Name;
            context.SaveChanges();
            
            await UpdateGroupsAsync();
            SelectedGroup = Groups.Cast<Group>().First(g => g.Id == s.Id);
            SaveGroupCommand.NotifyCanExecute(true);
        }

        private void ShowChangeGroupGroupType(Group group)
        {
            var pipe = new Pipe(true);
            pipe.SetParameter("group", group);
            StartViewModel<ChangeGroupGroupTypeViewModel>(pipe);

            var res = (Groups?)pipe.GetParameter("group_type_result");
            if (!res.HasValue) return;

            var previousType = group.GroupType;

            group.GroupType = res.Value;
//
//            // analog of updating of groups
            var s = SelectedGroup;
            var g = Groups;
            Groups = null;
            Groups = g;
            SelectedGroup = s;

            SaveDocumentGroupChangeType(group, previousType);
        }

        private void SaveDocumentGroupChangeType(Group group, Groups previousType)
        {
            var sfd = new SaveFileDialog {Filter = "Word|*.docx", FileName = AppFilePaths.GetGroupTypeChangedFileName() };
            if (sfd.ShowDialog() != true) return;

            var children = Children.Cast<Child>().Where(c => c.GroupId == @group.Id);
            var body = new List<IDictionary<string, string>>(4);
            body.AddRange(children.Select(MakeSimpleDataForDocument));

            var now = DateTime.Now;
            var head = new Dictionary<string, string>
            {
                ["&date_full"] = now.ToString(OtherSettings.DateFormat),
                ["&date_d"] = now.Day.ToString(),
                ["&date_m"] = now.Month.ToString(),
                ["&date_y"] = now.Year.ToString(),
                ["&group_prev_type"] = ConvertGroupType(previousType),
                ["&group_id"] = group.Id.ToString(),
                ["&group_name"] = group.Name,
                ["&group_type"] = ConvertGroupType(group.GroupType),
            };

            var src = AppFilePaths.GetGroupTypeChangedTemplatePath();
            WordWorker.InsertTableAndReplaceText(Path.GetFullPath(src), sfd.FileName, body, head);
        }

        private static string ConvertGroupType(Groups groupType)
        {
            if (_groupConverter == null)
            {
                _groupConverter = (IValueConverter) Application.Current.FindResource("GroupsConverter");
                Debug.Assert(_groupConverter != null, "_groupConverter != null");
            }
            return (string) _groupConverter.Convert(groupType, typeof (Groups), null, CultureInfo.CurrentCulture);
        }

        private static Dictionary<string, string> MakeSimpleDataForDocument(Child child)
            => MakeSimpleDataForDocument(child, child.Group);
        private static Dictionary<string, string> MakeSimpleDataForDocument(Child child, Group group)
        {
            var now = DateTime.Now;
            if (group == null) group = child.Group;
            string groupType = group == null ? null : ConvertGroupType(group.GroupType);

            return new Dictionary<string, string>
            {
                ["&date_d"] = now.Day.ToString(),
                ["&date_m"] = now.Month.ToString(),
                ["&date_y"] = now.Year.ToString(),
                ["&date_full"] = now.ToString(OtherSettings.DateFormat),

                ["&child_id"] = child.Id.ToString(),
                ["&child_first_name"] = child.Person.FirstName,
                ["&child_second_name"] = child.Person.LastName,
                ["&child_patronymic"] = child.Person.Patronymic,
                ["&child_full_name"] = child.Person.FullName,
                ["&child_birthdate"] = child.BirthDate.ToString(OtherSettings.DateFormat),

                ["&group_id"] = @group?.Id.ToString(),
                ["&group_name"] = @group?.Name,
                ["&group_type"] = groupType,

                ["&tarif_id"] = child.TarifId.ToString(),
                ["&tarif_annual_payment"] = child.Tarif?.AnnualPayment.Str(),
                ["&tarif_monthly_payment"] = child.Tarif?.MonthlyPayment.Str(),
                ["&tarif_note"] = child.Tarif?.Note,
            };
        }

        private async void ShowAddGroupWindow()
        {
            ShowAddGroupCommand.NotifyCanExecute(false);
            var pipe = new Pipe(true);
            try
            {
                StartViewModel<AddGroupViewModel>(pipe);
            }
            finally
            {
                ShowAddGroupCommand.NotifyCanExecute(true);
            }
            var group = (Group)pipe.GetParameter("added_group_result");
            if (group != null)
            {
                await UpdateGroupsAsync();
                SelectedGroup = Groups.Cast<Group>().First(g => g.Id == group.Id);
            }
        }

        private void ShowAddNewTarif()
        {
            ShowAddNewTarifCommand.NotifyCanExecute(false);
            var pipe = new Pipe(true);
            try
            {
                StartViewModel<AddTarifViewModel>(pipe);
            }
            finally
            {
                ShowAddNewTarifCommand.NotifyCanExecute(true);
            }
            var tarif = (Tarif)pipe.GetParameter("tarif_result");
            if (tarif != null)
            {
                Tarifs.Add(tarif);
                SelectedTarifClone = tarif;
            }
        }

        private async void DeleteSelectedTarif()
        {
            if (SelectedTarif == null) return;
            if (SelectedTarif.ChildCount > 0)
            {
                MessageBox.Show($"Данным тарифом пользуются дети ({SelectedTarif.ChildCount})", "Неверное удаление");
                return;
            }

            if (MessageBox.Show("Точно удалить?", "Удаление тарифа", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.Tarifs.Remove(context.Tarifs.First(t => t.Id == SelectedTarif.Id));
                context.SaveChanges();
            });
            Tarifs.Remove(SelectedTarif);
            SelectedTarifClone = null;
        }

        private async void ShowAddChildWindow()
        {
            ShowAddChildCommand.NotifyCanExecute(false);
            var pipe = new Pipe(true);
            pipe.SetParameter("groups", Groups.Cast<Group>().ToList());
            pipe.SetParameter("tarifs", Tarifs);

            try
            {
                StartViewModel<AddChildViewModel>(pipe);
            }
            finally
            {
                ShowAddChildCommand.NotifyCanExecute(true);
            }

            var addedChild = (Child)pipe.GetParameter("saved_child_result");
            if (addedChild != null)
            {
                var enterDate = addedChild.LastEnterChild.EnterDate;
                if (enterDate < FromEnterDateChildrenFilter) FromEnterDateChildrenFilter = enterDate;
                var mResult = MessageBox.Show("Ребёнок добавлен.\r\nОткрыть портфолио?", "Что дальше?", MessageBoxButton.YesNo);
                if (mResult == MessageBoxResult.Yes)
                {
                    ShowChildDetailsCommand.Execute(addedChild);
                }
                SaveDocumentAfterChildIsAdded(addedChild, (ParentChild)pipe.GetParameter("brought_parent"), (IList<ParentChild>)pipe.GetParameter("parents"),
                    ((EnterChild)pipe.GetParameter("enter")).EnterDate);
                await UpdateChildrenAsync();
            }
        }

        private void SaveDocumentAfterChildIsAdded(Child addedChild, ParentChild bringParentChild, IEnumerable<ParentChild> parents, DateTime enterDate)
        {
            Parent father = null, mother = null, other = null, bringParent = null;
            foreach (var parent in parents)
            {
                switch (parent.ParentType)
                {
                    case Parents.Father:
                        father = parent.Parent;
                        break;
                    case Parents.Mother:
                        mother = parent.Parent;
                        break;
                    case Parents.Other:
                        other = parent.Parent;
                        break;
                }
            }
            switch (bringParentChild.ParentType)
            {
                case Parents.Father:
                    bringParent = father;
                    break;
                case Parents.Mother:
                    bringParent = mother;
                    break;
                case Parents.Other:
                    bringParent = other;
                    break;
            }
            var group = Groups.Cast<Group>().First(g => g.Id == addedChild.GroupId);
            var tarif = Tarifs.First(t => t.Id == addedChild.TarifId);

            var data = MakeSimpleDataForDocument(addedChild, group);
            data["&child_enter_date"] = enterDate.ToString(OtherSettings.DateFormat);
            data["&group_id"] = @group.Id.ToString();
            data["&group_name"] = @group.Name;
            data["&parent_type"] = bringParentChild.ParentType == Parents.Father
                ? "Отец"
                : bringParentChild.ParentType == Parents.Mother
                    ? "Мать"
                    : bringParentChild.ParentTypeText;
            data["&bring_parent_second_name"] = bringParent?.Person?.LastName;
            data["&bring_parent_first_name"] = bringParent?.Person?.FirstName;
            data["&bring_parent_patronymic"] = bringParent?.Person?.Patronymic;
            data["&bring_parent_residence_address"] = bringParent?.ResidenceAddress;
            data["&bring_parent_location_address"] = bringParent?.LocationAddress;
            data["&bring_parent_work_address"] = bringParent?.WorkAddress;
            data["&bring_parent_phone_number"] = bringParent?.PhoneNumber;
            data["&bring_parent_passport_series"] = bringParent?.PassportSeries?.Substring(0, 4);
            data["&bring_parent_passport_number"] = bringParent?.PassportSeries?.Substring(4);
            data["&bring_parent_passport_issue_date"] = bringParent?.PassportIssueDate.ToString(OtherSettings.DateFormat);
            data["&bring_parent_passport_issue_by"] = bringParent?.PassportIssuedBy;
            data["&father_second_name"] = father?.Person?.LastName;
            data["&father_first_name"] = father?.Person?.FirstName;
            data["&father_patronymic"] = father?.Person?.Patronymic;
            data["&father_residence_address"] = father?.ResidenceAddress;
            data["&father_location_address"] = father?.LocationAddress;
            data["&father_work_address"] = father?.WorkAddress;
            data["&father_phone_number"] = father?.PhoneNumber;
            data["&father_passport_series"] = father?.PassportSeries?.Substring(0, 4);
            data["&father_passport_number"] = father?.PassportSeries?.Substring(4);
            data["&father_passport_issue_date"] = father?.PassportIssueDate.ToString(OtherSettings.DateFormat);
            data["&father_passport_issue_by"] = father?.PassportIssuedBy;
            data["&mother_second_name"] = mother?.Person?.LastName;
            data["&mother_first_name"] = mother?.Person?.FirstName;
            data["&mother_patronymic"] = mother?.Person?.Patronymic;
            data["&mother_residence_address"] = mother?.ResidenceAddress;
            data["&mother_location_address"] = mother?.LocationAddress;
            data["&mother_work_address"] = mother?.WorkAddress;
            data["&mother_phone_number"] = mother?.PhoneNumber;
            data["&mother_passport_series"] = mother?.PassportSeries.Substring(0, 4);
            data["&mother_passport_number"] = mother?.PassportSeries.Substring(4);
            data["&mother_passport_issue_date"] = mother?.PassportIssueDate.ToString(OtherSettings.DateFormat);
            data["&mother_passport_issue_by"] = mother?.PassportIssuedBy;
            data["&tarif_id"] = tarif.Id.ToString();
            data["&tarif_monthly_payment"] = tarif.MonthlyPayment.Str();
            data["&tarif_annual_payment"] = tarif.AnnualPayment.Str();
            data["&tarif_note"] = tarif.Note;

            var dirName = Path.GetFullPath(AppFilePaths.GetDocumentsDirectoryPathForChild(addedChild, enterDate.Year));
            Directory.CreateDirectory(dirName);
            CommonHelper.OpenFileOrDirectory(dirName);

            var path = addedChild.Person.PhotoPath == null ? null : Path.GetFullPath(Path.Combine(AppFilePaths.ChildImages, addedChild.Person.PhotoPath));

            // agreement
            WordWorker.Replace(Path.GetFullPath(AppFilePaths.GetAgreementTemplatePath()),
                               Path.Combine(dirName, AppFilePaths.GetAgreementFileName(addedChild)), data, path);
            // portfolio
            WordWorker.Replace(Path.GetFullPath(AppFilePaths.GetPortfolioTemplatePath()),
                               Path.Combine(dirName, AppFilePaths.GetPortfolioFileName(addedChild)), data, path);
            // taking_child
            WordWorker.Replace(Path.GetFullPath(AppFilePaths.GetTakingChildTemplatePath()),
                               Path.Combine(dirName, AppFilePaths.GetTakingChildFileName(addedChild)), data, path);
            // order_of_admission
            WordWorker.Replace(Path.GetFullPath(AppFilePaths.GetOrderOfAdmissionTemplatePath()),
                               Path.Combine(dirName, AppFilePaths.GetOrderOfAdmissionFileName(addedChild)), data, path);
        }

        private void ShowChildDetails(Child child)
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["child_id"] = child.Id,
                ["groups"] = Groups.Cast<Group>().ToList(),
                ["owner"] = this,
                ["tarifs"] = Tarifs,
            };
            StartViewModel<ChildDetailsViewModel>(new Pipe(parameters, false));
        }

        private async void Load()
        {
            Logger.Debug("Before updating MainViewModel");
            RefreshDataCommand.NotifyCanExecute(false);
            ++LoadingDataCount;

            await UpdateChildrenAsync();
            await UpdateGroupsAsync();
            await UpdateTarifsAsync();
            await UpdateExpensesAsync();
            await UpdateIncomesAsync();

            --LoadingDataCount;
            RefreshDataCommand.NotifyCanExecute(true);
            Logger.Debug("After updating MainViewModel");
        }
        private async Task UpdateIncomesAsync()
        {
            ++LoadingDataCount;

            _incomesList = null;
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                _incomesList = context.Incomes
                    .Select(inc => new IncomeDTO
                    {
                        Id = inc.Id,
                        PersonName = inc.PersonName,
                        IncomeDate = inc.IncomeDate,
                        Money = inc.Money,
                        Note = inc.Note,
                        Prefix = IncomePrefix,
                    })
                    .Union(context.MonthlyPayments.Include("Child.Person")
                    .Select(payment => new IncomeDTO
                    {
                        Id = payment.Id,
                        PersonName = payment.Child.Person.FirstName + " " + payment.Child.Person.LastName + " " + payment.Child.Person.Patronymic,
                        IncomeDate = payment.PaymentDate,
                        Money = payment.PaidMoney,
                        Note = payment.Description,
                        Prefix = MonthlyPrefix,
                    }).Where(m => m.Money != 0))
                    .Union(context.AnnualPayments.Include("Child.Person")
                    .Select(payment => new IncomeDTO
                    {
                        Id = payment.Id,
                        PersonName = payment.Child.Person.FirstName + " " + payment.Child.Person.LastName + " " + payment.Child.Person.Patronymic,
                        IncomeDate = payment.PaymentDate,
                        Money = payment.MoneyPaymentByTarif,
                        Note = payment.Description,
                        Prefix = AnnualPrefix,
                    }))
                    .ToList();
            });
            Incomes = new ListCollectionView(_incomesList);
            SelectedExpense = null;

            --LoadingDataCount;
        }

        private async Task UpdateExpensesAsync()
        {
            ++LoadingDataCount;

            ListCollectionView expenses = null;
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var list = context.Expenses.ToList();
                _minFromDateExpenses = list.Count > 0 ? (DateTime?) list.Min(e => e.ExpenseDate) : null;
                expenses = new ListCollectionView(list);
            });
            Expenses = expenses;
            if (_minFromDateExpenses != null) FromDateExpensesFilter = _minFromDateExpenses.Value;

            --LoadingDataCount;
            Logger.Trace("Expenses were updated");
        }

        public async Task UpdateChildrenAsync()
        {
            ++LoadingDataCount;

            DateTime from = DateTime.MaxValue;
            int notArchivedCount = 0;
            int archivedCount = 0;
            double debtSum = 0;

            var c = await Task.Run(() =>
            {
                var context = new KindergartenContext();
                //var children0 = context.Children
                //    .Include("Group")
                //    .Include("Person")
                //    .Include("Tarif")
                //    .Include("ParentsChildren.Parent")
                //    .Select(ch => new
                //    {
                //        child = ch,
                //        lastEnter = ch.EnterChildren.FirstOrDefault(en => en.EnterDate == ch.EnterChildren.Max(en2 => en2.EnterDate)),
                //        lastMonthlyPayment = ch.Payments.FirstOrDefault(p => p.PaymentDate == ch.Payments.Max(p2 => (DateTime?) p.PaymentDate))
                //    })
                //    .OrderByDescending(ch => ch.lastEnter.EnterDate)
                //    .ToList();
                var enters = context.EnterChildren
                    .Include("Child.Group")
                    .Include("Child.Person")
                    .Include("Child.Tarif")
                    .Include("Child.ParentsChildren.Parent")
                    .Where(e => e.EnterDate == context.EnterChildren.Where(t => t.ChildId == e.ChildId).Max(ee => ee.EnterDate))
                    .OrderByDescending(e => e.EnterDate)
                    .ToList();
                var childrenWithLastPayment = context
                    .Children
                    .Select(ch => new {
                        ch.Id,
                        lastPayment = ch.Payments.FirstOrDefault(p => p.PaymentDate == ch.Payments.Max(p2 => (DateTime?)p2.PaymentDate)),
                        lastEnter = ch.EnterChildren.Max(e => e.EnterDate)})
                    .OrderByDescending(ch => ch.lastEnter)
                    .ToList();
                var result = new List<Child>(8);
                int i = 0;
                foreach (var enter in enters)
                {
                    var child = enter.Child;
                    //var child = enter.child;
                    child.LastEnterChild = enter;
                    //child.LastEnterChild = enter.lastEnter;
                    var lmp = childrenWithLastPayment[i++].lastPayment;
                    if (lmp != null)
                    {
                        var endDate = enter.ExpulsionDate ?? DateTime.Now;
                        if (endDate < lmp.PaymentDate) // if there's past date
                            endDate = lmp.PaymentDate;
                        double debt = lmp.DebtAfterPaying +
                                          lmp.MoneyPaymentByTarif*((endDate.Year-lmp.PaymentDate.Year)*12 + endDate.Month-lmp.PaymentDate.Month);
                        debtSum += debt;
                        child.MonthlyDebt = debt;
                        child.LastMonthlyPayment = lmp;
                    }
                    //child.LastMonthlyPayment = enter.lastMonthlyPayment;
                    if (child.LastEnterChild.ExpulsionDate != null) archivedCount++;

                    DateTime enterDate = child.LastEnterChild.EnterDate;
                    if (from > enterDate) from = enterDate;

                    result.Add(child);
                }
                notArchivedCount = result.Count - archivedCount;

                return result;
            });
            CommonDebt = debtSum;
            if (c.Count > 0)
                if (!FromEnterDateChildrenFilter.HasValue) FromEnterDateChildrenFilter = from;

            var selectedChild = SelectedChild;
//            var listCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(c);
            var list = new ListCollectionView(c);
            Children = list;
            if (selectedChild != null && c.Count > 0)
                SelectedChild = c.FirstOrDefault(ch => ch.Id == selectedChild.Id);

            ChildNoArchivedCount = notArchivedCount;
            ChildTotalCount = c.Count;

            --LoadingDataCount;
            Logger.Trace("Children were updated");
        }

        public async Task UpdateGroupsAsync()
        {
            ++LoadingDataCount;
            Groups = await Task.Run(() =>
            {
                var context = new KindergartenContext();

                var innerGroupIdAndCount = context
                    .Children
                    .Join(context.EnterChildren.Where(e => e.ExpulsionDate == null), c => c.Id, e => e.ChildId, (c, e) => new {c, e})
                    .GroupBy(g => g.c.GroupId)
                    .Select(group => new { groupId=group.Key, count=group.Count()});

                var queryable =
                    from g in context.Groups
                    join gc in innerGroupIdAndCount on g.Id equals gc.groupId into gc2
                    from sub in gc2.DefaultIfEmpty()
                    select new { @group=g, count = sub == null ? 0 : sub.count };

                var groups = new List<Group>(8);
                foreach (var g in queryable)
                {
                    g.group.ChildCount = g.count;
                    groups.Add(g.group);
                }
                return new ListCollectionView(groups);
            });
            --LoadingDataCount;
            Logger.Trace("Groups were updated");
        }

        public async Task UpdateTarifsAsync()
        {
            ++LoadingDataCount;
            Tarifs = await Task.Run(() =>
            {
                var result = new ObservableCollection<Tarif>();
                var tcs = new KindergartenContext().Tarifs.Select(t => new {tarif = t, ChildCount = t.Children.Count,});
                foreach (var tc in tcs)
                {
                    tc.tarif.ChildCount = tc.ChildCount;
                    result.Add(tc.tarif);
                }
                return result;
            });
            --LoadingDataCount;
            Logger.Trace("Tarifs were updated");
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
                if (ArchivedChildrenFilter.Value == (c.LastEnterChild.ExpulsionDate == null))
                    return false;
            }

            // EnterDate
            if (FromEnterDateChildrenFilter > c.LastEnterChild.EnterDate || TillEnterDateChildrenFilter < c.LastEnterChild.EnterDate.Date)
                return false;

            if (OnlyDebtorsChildrenFilter && c.LastMonthlyPayment != null && c.MonthlyDebt <= 0)
            {
                return false;
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
            if (!string.IsNullOrEmpty(GroupNameChildrenFilter) &&
                c.Group.Name.IndexOf(GroupNameChildrenFilter, StringComparison.InvariantCultureIgnoreCase) < 0)
                return false;
            return true;
        }

        private void OnPropertyChangedAndRefreshChildrenFilter([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
            if (Children == null) return;
            Children.TryRefreshFilter();
            CommonDebt = (
                from Child c in Children
                where c.MonthlyDebt != null
                select c.MonthlyDebt.Value
                ).Sum();
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
                var min = _tillEnterDateChildrenFilter < value ? _tillEnterDateChildrenFilter : value;
                _fromEnterDateChildrenFilter = min;
                OnPropertyChangedAndRefreshChildrenFilter();
            }
        }

        public DateTime? TillEnterDateChildrenFilter
        {
            get { return _tillEnterDateChildrenFilter; }
            set
            {
                if (value.Equals(_tillEnterDateChildrenFilter)) return;
                var max = _fromEnterDateChildrenFilter > value ? _fromEnterDateChildrenFilter : value;
                _tillEnterDateChildrenFilter = max;
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

        public string GroupNameChildrenFilter
        {
            get { return _groupNameChildrenFilter; }
            set
            {
                if (value == _groupNameChildrenFilter) return;
                _groupNameChildrenFilter = value;
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

        public ListCollectionView Groups
        {
            get { return _groups; }
            private set
            {
                if (Equals(value, _groups)) return;
                _groups = value;
                if (_groups != null)
                    _groups.Filter = o =>
                        !ShowGroupsFromArchive.HasValue ||
                        ShowGroupsFromArchive.Value == ((((Group) o).GroupType & DAL.Model.Groups.Finished) != 0);
                OnPropertyChanged();
            }
        }

        public bool? ShowGroupsFromArchive
        {
            get { return _showGroupsFromArchive; }
            set
            {
                if (value == _showGroupsFromArchive) return;
                this[nameof(ShowGroupsFromArchive)] = _showGroupsFromArchive = value;
                OnPropertyChanged();
                _groups.TryRefreshFilter();
            }
        }

        public ListCollectionView Children
        {
            get { return _children; }
            private set
            {
                if (Equals(value, _children)) return;
                _children = value;
                if (_children != null) _children.Filter = ChildFilter;
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

        public double CommonDebt
        {
            get { return _commonDebt; }
            set
            {
                if (value.Equals(_commonDebt)) return;
                _commonDebt = value;
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

        public Tarif SelectedTarif
        {
            get { return _selectedTarif; }
            set
            {
                if (Equals(value, _selectedTarif)) return;
                _selectedTarif = value;
                OnPropertyChanged();
                SelectedTarifClone = value;
            }
        }

        public Tarif SelectedTarifClone
        {
            get { return _selectedTarifClone; }
            set
            {
                if (Equals(value, _selectedTarifClone)) return;
                _selectedTarifClone = value?.Clone();
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

        public bool IsDataLoading
        {
            get { return _isDataLoading; }
            set
            {
                if (value == _isDataLoading) return;
                _isDataLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotDataLoading));
            }
        }

        public ListCollectionView Expenses
        {
            get { return _expenses; }
            set
            {
                if (Equals(value, _expenses)) return;
                _expenses = value;
                _expenses.Filter = o =>
                {
                    var expense = (Expense) o;

                    if (!string.IsNullOrEmpty(SearchExpensesFilter) && expense.Description.IndexOf(SearchExpensesFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;

                    if (FromDateExpensesFilter.HasValue && expense.ExpenseDate < FromDateExpensesFilter.Value)
                        return false;

                    if (TillDateExpensesFilter.HasValue && expense.ExpenseDate < TillDateExpensesFilter.Value)
                        return false;

                    if (SelectedExpenseTypesExpensesFilter.HasValue && expense.ExpenseType != SelectedExpenseTypesExpensesFilter)
                        return false;

                    return true;
                };
                _expenses.CurrentChanged += (s, e) => SumOfSumOfExpensesFilter = _expenses.Cast<Expense>().Sum(ex => ex.Money);
                OnPropertyChanged();
            }
        }

        public Expense SelectedExpense
        {
            get { return _selectedExpense; }
            set
            {
                if (Equals(value, _selectedExpense)) return;
                _selectedExpense = value;
                OnPropertyChanged();
            }
        }

        public string SearchExpensesFilter
        {
            get { return _searchExpensesFilter; }
            set
            {
                if (value == _searchExpensesFilter) return;
                _searchExpensesFilter = value;
                Expenses.TryRefreshFilter();
                OnPropertyChanged();
            }
        }

        public DateTime? FromDateExpensesFilter
        {
            get { return _fromDateExpensesFilter; }
            set
            {
                if (value.Equals(_fromDateExpensesFilter)) return;
                _fromDateExpensesFilter = value;
                Expenses.TryRefreshFilter();
                OnPropertyChanged();
            }
        }

        public DateTime? TillDateExpensesFilter
        {
            get { return _tillDateExpensesFilter; }
            set
            {
                if (value.Equals(_tillDateExpensesFilter)) return;
                _tillDateExpensesFilter = value;
                Expenses.TryRefreshFilter();
                OnPropertyChanged();
            }
        }

        public double SumOfSumOfExpensesFilter
        {
            get { return _sumOfSumOfExpensesFilter; }
            set
            {
                if (value.Equals(_sumOfSumOfExpensesFilter)) return;
                _sumOfSumOfExpensesFilter = value;
                OnPropertyChanged();
            }
        }

        public ExpenseType? SelectedExpenseTypesExpensesFilter
        {
            get { return _selectedExpenseTypesExpensesFilter; }
            set
            {
                if (value == _selectedExpenseTypesExpensesFilter) return;
                _selectedExpenseTypesExpensesFilter = value;
                Expenses.TryRefreshFilter();
                OnPropertyChanged();
            }
        }

        public string SearchIncomesFilter
        {
            get { return _searchIncomesFilter; }
            set
            {
                if (value == _searchIncomesFilter) return;
                _searchIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        public DateTime? FromDateIncomesFilter
        {
            get { return _fromDateIncomesFilter; }
            set
            {
                if (value.Equals(_fromDateIncomesFilter)) return;
                _fromDateIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        public DateTime? TillDateIncomesFilter
        {
            get { return _tillDateIncomesFilter; }
            set
            {
                if (value.Equals(_tillDateIncomesFilter)) return;
                _tillDateIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        public double SumOfSumOfIncomesFilter
        {
            get { return _sumOfSumOfIncomesFilter; }
            set
            {
                if (value.Equals(_sumOfSumOfIncomesFilter)) return;
                _sumOfSumOfIncomesFilter = value;
                OnPropertyChanged();
            }
        }

        public bool IsShowedOnlyIncomesFilter
        {
            get { return _isShowedOnlyIncomesFilter; }
            set
            {
                if (value == _isShowedOnlyIncomesFilter) return;
                this[nameof(IsShowedOnlyIncomesFilter)] = _isShowedOnlyIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        public bool IsShowedMonthlyIncomesFilter
        {
            get { return _isShowedMonthlyIncomesFilter; }
            set
            {
                if (value == _isShowedMonthlyIncomesFilter) return;
                this[nameof(IsShowedMonthlyIncomesFilter)] = _isShowedMonthlyIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        public bool IsShowedAnnualIncomesFilter
        {
            get { return _isShowedAnnualIncomesFilter; }
            set
            {
                if (value == _isShowedAnnualIncomesFilter) return;
                this[nameof(IsShowedAnnualIncomesFilter)] = _isShowedAnnualIncomesFilter = value;
                OnPropertyChanged();
                Incomes.TryRefreshFilter();
            }
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local"), SuppressMessage("ReSharper", "InconsistentNaming")]
        public class IncomeDTO
        {
            public int Id { get; set; }
            public string PersonName { get; set; }
            public DateTime IncomeDate { get; set; }
            public double Money { get; set; }
            public string Note { get; set; }
            public string Prefix { get; set; }
        }

        public ListCollectionView Incomes
        {
            get { return _incomes; }
            set
            {
                if (Equals(value, _incomes)) return;
                _incomes = value;
                _incomes.Filter = o =>
                {
                    var income = (IncomeDTO)o;
                    if (!string.IsNullOrEmpty(SearchIncomesFilter) &&
                        (income.PersonName == null ||
                         income.PersonName.IndexOf(SearchIncomesFilter, StringComparison.OrdinalIgnoreCase) < 0) &&
                        (income.Note == null ||
                         income.Note.IndexOf(SearchIncomesFilter, StringComparison.OrdinalIgnoreCase) < 0))
                        return false;

                    if (FromDateIncomesFilter.HasValue && income.IncomeDate < FromDateIncomesFilter)
                        return false;

                    if (TillDateIncomesFilter.HasValue && income.IncomeDate > TillDateIncomesFilter)
                        return false;

                    switch (income.Prefix)
                    {
                        case IncomePrefix:
                            if (!IsShowedOnlyIncomesFilter) return false;
                            break;
                        case MonthlyPrefix:
                            if (!IsShowedMonthlyIncomesFilter) return false;
                            break;
                        case AnnualPrefix:
                            if (!IsShowedAnnualIncomesFilter) return false;
                            break;
                    }

                    return true;
                };
                EventHandler onCurrentChanged = (s, e) =>
                    SumOfSumOfIncomesFilter = ((ListCollectionView)s)
                        .Cast<object>()
                        .Where(i => i is IncomeDTO)
                        .Cast<IncomeDTO>()
                        .Sum(i => i.Money);
                _incomes.CurrentChanged += onCurrentChanged;
                onCurrentChanged(value, EventArgs.Empty);
                OnPropertyChanged();
            }
        }

        public bool CanDeleteIncome
        {
            get { return _canDeleteIncome; }
            set
            {
                if (value == _canDeleteIncome) return;
                _canDeleteIncome = value;
                OnPropertyChanged();
            }
        }

        public IncomeDTO SelectedIncome
        {
            get { return _selectedIncome; }
            set
            {
                if (Equals(value, _selectedIncome)) return;
                _selectedIncome = value;
                OnPropertyChanged();
                CanDeleteIncome = value != null && value.Prefix == IncomePrefix;
            }
        }

        public bool IsNotDataLoading => !IsDataLoading;

        public IRelayCommand ShowAddGroupCommand { get; }
        public IRelayCommand ShowAddChildCommand { get; }
        public IRelayCommand ShowChildDetailsCommand { get; }
        public IRelayCommand RefreshDataCommand { get; }
        public IRelayCommand ShowAddNewTarifCommand { get; }
        public IRelayCommand DeleteSelectedTarifCommand { get; }
        public IRelayCommand ChangeGroupGroupTypeCommand { get; }
        public IRelayCommand SaveGroupCommand { get; }
        public IRelayCommand SaveTarifCommand { get; }
        public IRelayCommand GroupToggleArchiveCommand { get; }
        public IRelayCommand ShowAddExpenseCommand { get; }
        public IRelayCommand ResetExpensesFilterCommand { get; }
        public IRelayCommand RemoveExpenseCommand { get; }
        public IRelayCommand AddIncomeCommand { get; }
        public IRelayCommand ResetIncomesFilterCommand { get; }
        public IRelayCommand RemoveSelectedIncomeCommand { get; }
        public IRelayCommand PrintDocumentChildrenCommand { get; }

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
        private bool? _archivedChildrenFilter = false;
        private bool _onlyDebtorsChildrenFilter;
        private string _title;
        private ListCollectionView _children;
        private ListCollectionView _groups;
        private Child _selectedChild;
        private DateTime? _fromEnterDateChildrenFilter;
        private DateTime? _tillEnterDateChildrenFilter;
        private bool _isDataLoading;
        private int _loadingDataCount;
        private int _childTotalCount;
        private int _childNoArchivedCount;
        private ObservableCollection<Tarif> _tarifs;
        private Tarif _selectedTarif;
        private Group _selectedGroup;
        private Tarif _selectedTarifClone;
        private bool? _showGroupsFromArchive = false;
        private string _groupNameChildrenFilter;
        private ListCollectionView _expenses;
        private string _searchExpensesFilter;
        private DateTime? _fromDateExpensesFilter;
        private DateTime? _tillDateExpensesFilter;
        private ExpenseType? _selectedExpenseTypesExpensesFilter;
        private DateTime? _minFromDateExpenses;
        private double _sumOfSumOfExpensesFilter;
        private ListCollectionView _incomes;
        private string _searchIncomesFilter;
        private DateTime? _fromDateIncomesFilter;
        private DateTime? _tillDateIncomesFilter;
        private double _sumOfSumOfIncomesFilter;
        private List<IncomeDTO> _incomesList;
        private bool _isShowedOnlyIncomesFilter;
        private bool _isShowedMonthlyIncomesFilter;
        private bool _isShowedAnnualIncomesFilter;
        private IncomeDTO _selectedIncome;
        private bool _canDeleteIncome;
        private Expense _selectedExpense;
        private double _commonDebt;
        private SaveFileDialog _childrenDocumentFileDialog;
        private static IValueConverter _groupConverter;

        // prefixes at Incomes tab
        private const string IncomePrefix = "Д-";
        private const string MonthlyPrefix = "М-";
        private const string AnnualPrefix = "Г-";

        #endregion
    }
}