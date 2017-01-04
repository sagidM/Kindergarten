using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.Util;
using WpfApp.View.DialogService;
using WpfApp.View.UI;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WpfApp.Settings;

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
        public IRelayCommand PayFeeCommand { get; }
        public IRelayCommand PayForYearCommand { get; }
        public IRelayCommand PayTillDateWithRecalculateCommand { get; }
        public IRelayCommand ReloadImageFromFileCommand { get; }
        public IRelayCommand RecaptureImageFromCameraCommand { get; }
        public IRelayCommand RemoveImageCommand { get; }
        public IRelayCommand OpenImageCommand { get; }
        public IRelayCommand OpenDocumentDirectoryCommand { get; }
        public IRelayCommand SaveDocumentReceiptForMonthlyPaymentCommand { get; }
        public IRelayCommand SaveDocumentReceiptForAnnualPaymentCommand { get; }

        public ChildDetailsViewModel()
        {
            SaveChangesCommand = new RelayCommand(SaveChanges);
            ChangeGroupCommand = new RelayCommand(ChangeGroup);
            AddChildToArchiveCommand = new RelayCommand<string>(AddChildToArchive);
            RemoveChildFromArchiveCommand = new RelayCommand(RemoveChildFromArchive);
            AttachParentCommand = new RelayCommand<Parents>(AttachParent);
            DetachParentCommand = new RelayCommand<Parents>(DetachParent);
            PayFeeCommand = new RelayCommand<string>(PayFee);
            PayForYearCommand = new RelayCommand(PayForYear);
            PayTillDateWithRecalculateCommand = new RelayCommand(PayTillDateWithRecalculate);
            ReloadImageFromFileCommand = new RelayCommand(ReloadImageFromFile);
            RecaptureImageFromCameraCommand = new RelayCommand(RecaptureImageFromCamera);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            OpenImageCommand = new RelayCommand(OpenImage);
            OpenDocumentDirectoryCommand = new RelayCommand(OpenDocumentDirectory);
            SaveDocumentReceiptForMonthlyPaymentCommand = new RelayCommand<MonthlyPayment>(SaveDocumentReceiptForMonthlyPayment);
            SaveDocumentReceiptForAnnualPaymentCommand = new RelayCommand<RangePayment>(SaveDocumentReceiptForAnnualPayment);

            _childNotifier = new DirtyPropertyChangeNotifier();
            _childNotifier.StartTracking();
            _fatherNotifier = new DirtyPropertyChangeNotifier();
            _fatherNotifier.StartTracking();
            _motherNotifier = new DirtyPropertyChangeNotifier();
            _motherNotifier.StartTracking();
            _otherNotifier = new DirtyPropertyChangeNotifier();
            _otherNotifier.StartTracking();

            Action onDirtyCountChanged = () => OnPropertyChanged(nameof(DirtyCount));
            _childNotifier.DirtyCountChanged += onDirtyCountChanged;
            _fatherNotifier.DirtyCountChanged += onDirtyCountChanged;
            _motherNotifier.DirtyCountChanged += onDirtyCountChanged;
            _otherNotifier.DirtyCountChanged += onDirtyCountChanged;
        }

        private void SaveDocumentReceiptForAnnualPayment(RangePayment p)
        {
            if (_saveDocumentDialog == null)
                _saveDocumentDialog = new SaveFileDialog
                {
                    Filter = "Word|*.docx",
                    FileName = AppFilePaths.GetAnnualReceiptFileName(CurrentChild)
                };
            if (_saveDocumentDialog.ShowDialog() != true) return;

            var now = DateTime.Now;
            var data = new Dictionary<string, string>
            {
                ["&date_d"] = now.Day.ToString(),
                ["&date_m"] = now.Month.ToString(),
                ["&date_y"] = now.Year.ToString(),
                ["&date_full"] = now.ToString(OtherSettings.DateFormat),
                ["&child_second_name"] = CurrentChild.Person.LastName,
                ["&child_first_name"] = CurrentChild.Person.FirstName,
                ["&child_patronymic"] = CurrentChild.Person.Patronymic,
                ["&child_full_name"] = CurrentChild.Person.FullName,
                ["&child_birthdate"] = CurrentChild.BirthDate.ToString(OtherSettings.DateFormat),

                ["&group_id"] = CurrentChildGroup.Id.ToString(),
                ["&group_name"] = CurrentChildGroup.Name,

                ["&payment_id"] = "Г-" + p.Id,
                ["&payment_sum"] = p.MoneyPaymentByTarif.Str(),
                ["&payment_date_from"] = p.PaymentFrom.ToString(OtherSettings.DateFormat),
                ["&payment_date_to"] = p.PaymentTo.ToString(OtherSettings.DateFormat),
                ["&payment_date"] = p.PaymentDate.ToString(OtherSettings.DateFormat),
                ["&payment_note"] = p.Description,
            };
            var src = Path.GetFullPath(AppFilePaths.GetAnnualReceiptTemplatePath());
            WordWorker.Replace(src, _saveDocumentDialog.FileName, data);
        }

        private void OpenDocumentDirectory()
        {
            var s = Path.GetFullPath(_documentDirectoryPath);
            if (!Directory.Exists(s))
            {
                if (MessageBox.Show("Папки с документами не существует, создать её?", "Отсутствие пакета документов", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
                Directory.CreateDirectory(s);
            }
            CommonHelper.OpenFileOrDirectory(s);
        }

        private void OpenImage()
        {
            if (_imageUri != null)
                CommonHelper.OpenFileOrDirectoryWithSelected(Path.GetFullPath(_imageUri.AbsolutePath));
        }

        private void RemoveImage()
        {
            if (CurrentChildPeoplePhotoPath == null) return;

            _isNewPhoto = true;
            _imageUri = null;
            CurrentChildPeoplePhotoPath = null;
            ChildImageSource = null;
        }

        // copypaste in AddChildViewModel.cs
        private void RecaptureImageFromCamera()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = App.WebcamPath, RedirectStandardOutput = true, UseShellExecute = false, Arguments = App.WebcamArguments,
                },
            };
            process.Start();
            var imagePath = process.StandardOutput.ReadLine();
            if (imagePath != null)
            {
                _isNewPhoto = true;
                CurrentChildPeoplePhotoPath = CommonHelper.GetUniqueString();
                SetChildImage(imagePath);
            }
        }

        private void SetChildImage(string path)
        {
            _imageUri = new Uri(path);
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                b.UriSource = _imageUri;
                b.EndInit();
                ChildImageSource = b;
            }
            catch (Exception e)
            {
                App.Logger.Warn(e, "Image is not supported. The path: " + path);
                _imageUri = null;
                var message = File.Exists(path) ? "Изображение не поддерживается" : $"Файл {path} не найден";
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ReloadImageFromFile()
        {
            if (_openFileDialog.ShowDialog() == false) return;

            var path = _openFileDialog.FileName;
            _isNewPhoto = true;
            CurrentChildPeoplePhotoPath = CommonHelper.GetUniqueString();
            SetChildImage(path);
        }

        private async void PayTillDateWithRecalculate()
        {
            if (RecalculationAnnualPaymentDate <= DateTime.Today)
            {
                MessageBox.Show("Перерасчёт допускается делать только на будущую дату", "Перерасчёт", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mbox = MessageBox.Show("Вы уверены, что хотите сделать перерасчёт?", "Перерасчёт", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (mbox != MessageBoxResult.Yes)
                return;

            PayTillDateWithRecalculateCommand.NotifyCanExecute(false);

            RangePayment payment = null;
            int res = await Task.Run(() =>
            {
                var context = new KindergartenContext();
                var payments = context.AnnualPayments.Where(p => p.ChildId == CurrentChild.Id).OrderBy(p => p.PaymentDate).ToList();

                var startDate = GetLastAnnualPaymentDate(payments);
                var endDate = RecalculationAnnualPaymentDate;

                if (startDate == endDate) return 0;  // till this date already paid

                // if difference between last payment and now is more than one year,
                // ask to pay for those years (mbox is below)
                var afterYear = startDate.AddYears(1);
                if (afterYear < endDate)
                {
                    var years = new DateTime((endDate - afterYear).Ticks).Year;
                    if (years > 0) return years;
                }

                var paidDays = (endDate - startDate).Days;
                payment = new RangePayment
                {
                    PaymentFrom = startDate,
                    PaymentTo = endDate,
                    // TODO: calc average days in year instead 365.25
                    MoneyPaymentByTarif = Math.Round(CurrentChildTarif.AnnualPayment / 365.25 * paidDays),
                    ChildId = CurrentChild.Id,
                    Description = AnnualPaymentDescription,
                    PaymentDate = DateTime.Now,
                };

                context.AnnualPayments.Add(payment);
                context.SaveChanges();
                return 0;
            });

            if (res > 0)
            {
                var yearWord = CommonHelper.GetRightRussianWord(res, "год", "года", "лет");
                MessageBox.Show($"Вам необходимо доплатить за {res} {yearWord}", "Недопустимая оплата",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                await LoadPayments();
            }

            if (payment != null) SaveDocumentReceiptForAnnualPaymentCommand.Execute(payment);
            PayTillDateWithRecalculateCommand.NotifyCanExecute(true);
        }

        private DateTime GetLastAnnualPaymentDate(IList<RangePayment> payments)
        {
            return payments.Count > 0 &&
                   payments[payments.Count - 1].PaymentTo > CurrentChild.LastEnterChild.EnterDate
                ? payments[payments.Count - 1].PaymentTo
                : CurrentChild.LastEnterChild.EnterDate;
        }

        private async void PayForYear()
        {
            if (Math.Abs(CurrentChildTarif.AnnualPayment) < 0.001)
            {
                MessageBox.Show("По текущему тарифу годовая оплата отсутствует!", "Пустой вызов");
                return;
            }

            DateTime fromDate = GetLastAnnualPaymentDate(PaymentsInYears);
            DateTime tillDate = fromDate.AddYears(1);

            MessageBoxResult mResult;
            if (fromDate <= DateTime.Now)
                mResult = MessageBox.Show("Вы уверены, что хотите оплатить за год?", "Годовая оплата", MessageBoxButton.YesNo);
            else
                mResult = MessageBox.Show("Текущий год оплачен!" +
                    Environment.NewLine +
                    "Вы уверены, что хотите произвести оплату за будущий год?",
                    "Годовая оплата", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (mResult != MessageBoxResult.Yes)
                return;

            PayForYearCommand.NotifyCanExecute(false);

            var payment = new RangePayment
            {
                PaymentFrom = fromDate,
                PaymentTo = tillDate,
                ChildId = CurrentChild.Id,
                MoneyPaymentByTarif = CurrentChildTarif.AnnualPayment,
                Description = AnnualPaymentDescription,
                PaymentDate = DateTime.Now,
            };
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.AnnualPayments.Add(payment);
                context.SaveChanges();
            });
            await LoadPayments();

            SaveDocumentReceiptForAnnualPaymentCommand.Execute(payment);
            PayForYearCommand.NotifyCanExecute(true);
        }

        public override async void OnLoaded()
        {
            int id = (int)Pipe.GetParameter("child_id");

            Child currentChild = null;
            Parent currentFather = null, currentMother = null, currentOther = null;
            IEnumerable<Group> groups = null;
            IEnumerable<Tarif> tarifs = null;
            MainViewModel mainViewModel = null;

            await Task.Run(() =>
            {
                _childContext = new KindergartenContext();
                var enters = _childContext.EnterChildren
                    .Include("Child.Person").Include("Child.Tarif").Include("Child.Group")
                    .Where(e => e.ChildId == id)
                    .ToList();
                var tmpContext = new KindergartenContext();
                currentFather = tmpContext.ParentChildren
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Father)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _fatherContext = currentFather != null ? tmpContext : null;

                tmpContext = new KindergartenContext();
                currentMother = tmpContext.ParentChildren.Include("Parent.Person")
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Mother)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _motherContext = currentMother != null ? tmpContext : null;

                tmpContext = new KindergartenContext();
                currentOther = tmpContext.ParentChildren.Include("Parent.Person")
                    .Where(pc => pc.ChildId == id && pc.ParentType == Parents.Other)
                    .Select(pc => pc.Parent)
                    .Include("Person")
                    .FirstOrDefault();
                _otherContext = currentOther != null ? tmpContext : null;

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
                if (maxEnterIndex == -1) App.Logger.Error("There's at least one enter");
                currentChild = enters[0].Child; // 0 - enters consists of same elements
                currentChild.LastEnterChild = enters[maxEnterIndex];
                _documentDirectoryPath = AppFilePaths.GetDocumentsDirectoryPathForChild(currentChild, enters[0].EnterDate.Year);

                groups = (IEnumerable<Group>)Pipe.GetParameter("groups");
                tarifs = (IEnumerable<Tarif>)Pipe.GetParameter("tarifs");
                mainViewModel = (MainViewModel)Pipe.GetParameter("owner");
            });

            CurrentChild = currentChild;
            CurrentFather = currentFather;
            CurrentMother = currentMother;
            CurrentOther = currentOther;
            Groups = groups;
            Tarifs = tarifs;
            _mainViewModel = mainViewModel;

            CurrentChildGroup = currentChild.Group;
            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentChildTarif));

            await LoadPayments();

            _childNotifier.SetProperty(nameof(CurrentChildTarif), CurrentChildTarif);
            _childNotifier.SetProperty("FatherId", CurrentFather?.Id);
            _childNotifier.SetProperty("MotherId", CurrentMother?.Id);
            _childNotifier.SetProperty("OtherId", CurrentOther?.Id);

            OnPropertyChanged(nameof(CurrentChild));
        }

        private async Task LoadPayments()
        {
            MonthlyPaymentsInYearsResult monthlyResult = null;
            ObservableCollection<RangePayment> annualResult = null;
            double totalAnnual = 0;
            await Task.Run(() =>
            {
                var id = CurrentChild.Id;
                // MonthlyPayments
                _paymentsContext = new KindergartenContext();
                monthlyResult = MonthlyPaymentsInYear.ToYears(
                    _paymentsContext.MonthlyPayments.Where(p => p.ChildId == id),
                    _paymentsContext.EnterChildren.Where(p => p.ChildId == id),
                    CurrentChild.Tarif);
                annualResult = new ObservableCollection<RangePayment>(_paymentsContext.AnnualPayments.Where(p => p.ChildId == CurrentChild.Id));
                var from = GetLastAnnualPaymentDate(annualResult);
                var now = DateTime.Now;
                totalAnnual = @from >= now
                    ? 0
                    : new DateTime((now - @from).Ticks).Year*CurrentChildTarif.AnnualPayment;
            });
            PaymentsInMonths = monthlyResult.MonthlyPaymentsInYears;
            PaymentsInYears = annualResult;
            LastMonthlyPayment = monthlyResult.LastPayment;
            TotalAnnualUnpaidMoney = totalAnnual;
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
                var savingResult = (SavingResult)savingPipe.GetParameter("saving_result");
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
            string text = null;
            if (parentType == Parents.Other)
            {
                text = IODialog.InputDialog("Кем приходится ребёнку", "Иной представитель", OtherParentText);
                if (text == null)
                    return;
            }

            var pipe = new Pipe(true);

            var exclude = new List<int>(3);
            if (CurrentFather != null) exclude.Add(CurrentFather.Id);
            if (CurrentMother != null) exclude.Add(CurrentMother.Id);
            if (CurrentOther != null) exclude.Add(CurrentOther.Id);
            pipe.SetParameter("exclude_parent_ids", exclude.ToArray());

            StartViewModel<AddParentViewModel>(pipe);
            var parent0 = (Parent)pipe.GetParameter("parent_result");
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

        private async void PayFee(string moneyStr)
        {
            double money;
            if (!double.TryParse(moneyStr, out money) || money <= 0)
            {
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите внести оплату в размере " + money + " рублей?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
            {
                return;
            }

            PayFeeCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                MonthlyPayment lastPayment;
                if (LastMonthlyPayment.Id != 0 && Math.Abs(LastMonthlyPayment.PaidMoney) < 0.001)
                {
                    // last payment is not fictitious, change it
                    lastPayment = LastMonthlyPayment;
                }
                else
                {
                    // add new payment
                    lastPayment = new MonthlyPayment {ChildId = CurrentChild.Id,};
                    _paymentsContext.MonthlyPayments.Add(lastPayment);
                }
                lastPayment.PaidMoney = money;
                lastPayment.Description = MonthlyPaymentDescription;
                lastPayment.MoneyPaymentByTarif = CurrentChildTarif.MonthlyPayment;
                lastPayment.DebtAfterPaying = LastMonthlyPayment.DebtAfterPaying - money;

                _paymentsContext.SaveChanges();
            });
            await LoadPayments();

            MonthlyPaymentMoney = string.Empty;

            SaveDocumentReceiptForMonthlyPaymentCommand.Execute(LastMonthlyPayment);
            PayFeeCommand.NotifyCanExecute(true);
        }

        private static SaveFileDialog _saveDocumentDialog;
        private void SaveDocumentReceiptForMonthlyPayment(MonthlyPayment payment)
        {
            if (_saveDocumentDialog == null)
                _saveDocumentDialog = new SaveFileDialog
                {
                    Filter = "Word|*.docx",
                    FileName = AppFilePaths.GetMonthlyReceiptFileName(CurrentChild)
                };
            if (_saveDocumentDialog.ShowDialog() != true) return;

            var now = DateTime.Now;
            var data = new Dictionary<string, string>
            {
                ["&date_d"] = now.Day.ToString(),
                ["&date_m"] = now.Month.ToString(),
                ["&date_y"] = now.Year.ToString(),
                ["&date_full"] = now.ToString(OtherSettings.DateFormat),
                ["&child_second_name"] = CurrentChild.Person.LastName,
                ["&child_first_name"] = CurrentChild.Person.FirstName,
                ["&child_patronymic"] = CurrentChild.Person.Patronymic,
                ["&child_full_name"] = CurrentChild.Person.FullName,
                ["&child_birthdate"] = CurrentChild.BirthDate.ToString(OtherSettings.DateFormat),

                ["&group_id"] = CurrentChildGroup.Id.ToString(),
                ["&group_name"] = CurrentChildGroup.Name,

                ["&payment_id"] = "М-" + payment.Id,
                ["&payment_sum"] = payment.PaidMoney.Str(),
                ["&payment_debt"] = payment.DebtAfterPaying.Str(),
                ["&payment_note"] = payment.Description,
            };
            var src = Path.GetFullPath(AppFilePaths.GetMonthlyReceiptTemplatePath());
            WordWorker.Replace(src, _saveDocumentDialog.FileName, data);
        }

        private async void SaveChanges()
        {
            SaveChangesCommand.NotifyCanExecute(false);

            if (CurrentFather == null && CurrentMother == null && CurrentOther == null)
            {
                MessageBox.Show("Должен быть выбран хотя бы один из родителей либо иной представитель", "Некорректный выбор", MessageBoxButton.OK, MessageBoxImage.Information);
                SaveChangesCommand.NotifyCanExecute(true);
                return;
            }

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
                            // Here must be right update query like this
                            // old.ParentId = currentParent.Id;
                            // but it doesn't work (maybe Entity thinks "Id" is auto increment)
                            context.ParentChildren.Remove(old);
                            context.ParentChildren.Add(new ParentChild { ChildId = CurrentChild.Id, ParentId = currentParent.Id, ParentType = type, ParentTypeText = old.ParentTypeText });
                            // select and insert instead of update
                        }
                    }
                };
                changeParent(father, CurrentFather, Parents.Father);
                changeParent(mother, CurrentMother, Parents.Mother);
                changeParent(other, CurrentOther, Parents.Other);

                // CurrentChild.TarifId is selected now; the other was selected before.
                // CurrentChild.Tarif can be null because "Tarifs" always has the same entities,
                // but "SaveChanges" set to null
                if (CurrentChild.Tarif == null || CurrentChild.Tarif.Id != CurrentChild.TarifId)
                {
                    // if tarif was exchanged, add fictition payment or change "MonthlyPayment" of current

                    var paymentDate = LastMonthlyPayment.PaymentDate;
                    var now = DateTime.Now;
                    if (LastMonthlyPayment.Id == 0 || paymentDate.Month != now.Month || paymentDate.Year != now.Year)
                    {
                        // add fictitious payment to change debt increase after this moment
                        CurrentChild.Payments.Add(new MonthlyPayment
                        {
                            Child = CurrentChild,
                            MoneyPaymentByTarif = CurrentChildTarif.MonthlyPayment,
                            PaidMoney = 0,
                            DebtAfterPaying = LastMonthlyPayment.DebtAfterPaying,
                        });
                    }
                    else
                    {
                        var last = CurrentChild.Payments.OrderBy(p => p.PaymentDate).Last();   // last payment was remitted in this month
                        last.MoneyPaymentByTarif = CurrentChildTarif.MonthlyPayment;
                    }
                }

                if (_isNewPhoto)
                {
                    if (_imageUri != null)
                    {
                        var filename = ImageUtil.SaveImage(_imageUri, AppFilePaths.ChildImages + Path.DirectorySeparatorChar + CurrentChildPeoplePhotoPath);
                        CurrentChildPeoplePhotoPath = Path.GetFileName(filename);
                    }
                    else
                    {
                        CurrentChildPeoplePhotoPath = null;
                    }
                }

                try
                {
                    context.SaveChanges();
                    _childContext.SaveChanges();
                    _fatherContext?.SaveChanges();
                    _motherContext?.SaveChanges();
                    _otherContext?.SaveChanges();

                    if (_isNewPhoto)
                    {
                        var oldOld = _oldPhotoSource;
                        _oldPhotoSource = CurrentChildPeoplePhotoPath;
                        _isNewPhoto = false;
                        if (oldOld != null) File.Delete(AppFilePaths.ChildImages + Path.DirectorySeparatorChar + oldOld);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error");
                    App.Logger.Error(e, "On save child details changes");
                }

                _childNotifier.ClearDirties();
                _fatherNotifier.ClearDirties();
                _motherNotifier.ClearDirties();
                _otherNotifier.ClearDirties();
            });
            await UpdateMainViewModel();
            SaveChangesCommand.NotifyCanExecute(true);
        }

        private async void AddChildToArchive(string note)
        {
            if (CurrentChildIsArchived) throw new InvalidOperationException();
            AddChildToArchiveCommand.NotifyCanExecute(false);

            EnterChild enter = null;
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                enter = context.EnterChildren.First(e => e.Id == CurrentChild.LastEnterChild.Id);
                enter.ExpulsionDate = DateTime.Now;
                enter.ExpulsionNote = note;
                context.SaveChanges();
            });

            CurrentChild.LastEnterChild = enter;

            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentChild));
            OnPropertyChanged(nameof(ExpulsionDateLastEnterChild));
            OnPropertyChanged(nameof(ExpulsionNoteLastEnterChild));

            var year = PaymentsInMonths[PaymentsInMonths.Count-1].Year;
            SaveDocumentAddingToArchive(CurrentChild, CurrentChildGroup, year);
            AddChildToArchiveCommand.NotifyCanExecute(true);
        }

        private static Dictionary<string, string> MakeDataForDocument(Child child, Group @group)
        {
            var groupType = (string)GroupConverter.Convert(@group.GroupType, @group.GroupType.GetType(), null, CultureInfo.CurrentCulture);
            var now = DateTime.Now;

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
                ["&child_location_address"] = child.LocationAddress,

                ["&group_id"] = child.GroupId.ToString(),
                ["&group_name"] = @group.Name,
                ["&group_type"] = groupType,
            };
        }

        public static void SaveDocumentAddingToArchive(Child child, Group group, int year)
        {
            var enter = child.LastEnterChild;
            if (enter.ExpulsionDate == null) throw new InvalidDataException();

            var data = MakeDataForDocument(child, group);
            data["&archive_adding_date"] = enter.ExpulsionDate.Value.ToString(OtherSettings.DateFormat);
            data["&archive_note"] = enter.ExpulsionNote;


            var src = AppFilePaths.GetAddingToArchiveTemplatePath();
            string dest = AppFilePaths.GetDocumentsDirectoryPathForChild(child, year) +
                          Path.DirectorySeparatorChar +
                          AppFilePaths.GetAddingToArchiveFileName(child, enter.ExpulsionDate.Value);
            dest = CommonHelper.ChangeFileNameIfFileExists(dest);
            
            WordWorker.Replace(Path.GetFullPath(src), Path.GetFullPath(dest), data);
        }

        private async void RemoveChildFromArchive()
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

            RemoveChildFromArchiveCommand.NotifyCanExecute(false);

            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.EnterChildren.Add(CurrentChild.LastEnterChild = new EnterChild {EnterDate = DateTime.Now, ChildId = CurrentChild.Id});
                context.SaveChanges();
            });
            OnPropertyChanged(nameof(CurrentChildIsArchived));
            OnPropertyChanged(nameof(CurrentChild));
            OnPropertyChanged(nameof(ExpulsionDateLastEnterChild));
            OnPropertyChanged(nameof(ExpulsionNoteLastEnterChild));

            SaveDocumentRemoveFromArchive();
            RemoveChildFromArchiveCommand.NotifyCanExecute(true);
        }

        private void SaveDocumentRemoveFromArchive()
        {
            var data = MakeDataForDocument(CurrentChild, CurrentChildGroup);
            var enterDate = CurrentChild.LastEnterChild.EnterDate;
            data["&child_enter_date"] = enterDate.ToString(OtherSettings.DateFormat);

            var year = PaymentsInMonths[PaymentsInMonths.Count - 1].Year;
            var src = AppFilePaths.GetTakingChildTemplatePath();
            string dest = AppFilePaths.GetDocumentsDirectoryPathForChild(CurrentChild, year) +
                          Path.DirectorySeparatorChar +
                          AppFilePaths.GetOrderOfAdmissionFileName(CurrentChild, enterDate);
            dest = CommonHelper.ChangeFileNameIfFileExists(dest);
            WordWorker.Replace(Path.GetFullPath(src), Path.GetFullPath(dest), data);
        }

        private async void ChangeGroup()
        {
            var prevGroup = CurrentChildGroup;
            var parameters = new Dictionary<string, object>(3)
            {
                ["child"] = CurrentChild,
                ["groups"] = Groups,
                ["current_group"] = Groups.First(g => g.Id == prevGroup.Id),
            };
            var pipe = new Pipe(parameters, true);
            StartViewModel<ChangeChildGroupViewModel>(pipe);
            var currentGroup = (Group)pipe.GetParameter("saved_group_result");
            if (currentGroup == null)
                return;
            
            CurrentChildGroup = currentGroup;

            SaveDocumentGroupTransfer(prevGroup);
            await UpdateMainViewModel();
        }

        private void SaveDocumentGroupTransfer(Group prevGroup)
        {
            var data = MakeDataForDocument(CurrentChild, CurrentChildGroup);
            data["&prev_group_id"] = prevGroup.Id.ToString();
            data["&prev_group_name"] = prevGroup.Name;
            var conv = GroupConverter;
            data["&prev_group_type"] = (string) conv.Convert(prevGroup.GroupType, prevGroup.GroupType.GetType(), null, CultureInfo.CurrentCulture);

            var groupType = (string) conv.Convert(CurrentChildGroup.GroupType, CurrentChildGroup.GroupType.GetType(), null, CultureInfo.CurrentCulture);

            int year = PaymentsInMonths[PaymentsInMonths.Count - 1].Year;
            string src = AppFilePaths.GetGroupTransferTemplatePath();
            string dest = AppFilePaths.GetDocumentsDirectoryPathForChild(CurrentChild, year) +
                          Path.DirectorySeparatorChar +
                          AppFilePaths.GetGroupTransferFileName(CurrentChild, DateTime.Now, CurrentChildGroup.Name, groupType);
            dest = CommonHelper.ChangeFileNameIfFileExists(dest);
            WordWorker.Replace(Path.GetFullPath(src), Path.GetFullPath(dest), data);
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

        public bool IsDirty => DirtyCount > 0;

        private int _bufferDirtyCount;
        public int DirtyCount
        {
            get
            {
                var dirtyCount = _childNotifier.DirtyFieldCount + _fatherNotifier.DirtyFieldCount +
                                 _motherNotifier.DirtyFieldCount + _otherNotifier.DirtyFieldCount;
                if (_bufferDirtyCount != dirtyCount)
                {
                    _bufferDirtyCount = dirtyCount;
                    OnPropertyChanged(nameof(IsDirty));
                }
                return dirtyCount;
            }
        }

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

                _oldPhotoSource = CurrentChildPeoplePhotoPath;
                if (_oldPhotoSource != null)
                    SetChildImage(Path.GetFullPath(Path.Combine(AppFilePaths.ChildImages, _oldPhotoSource)));
                OnPropertyChanged(nameof(CurrentChildPeoplePhotoPath));
                _childNotifier.SetProperty(nameof(CurrentChildPeoplePhotoPath), CurrentChildPeoplePhotoPath);
            }
        }

        public string CurrentChildPersonLastName
        {
            get { return CurrentChild?.Person?.LastName; }
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
            get { return CurrentChild?.Person?.FirstName; }
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
            get { return CurrentChild?.Person?.Patronymic; }
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
            get { return CurrentChild?.LocationAddress; }
            set
            {
                if (value == CurrentChild.LocationAddress) return;
                CurrentChild.LocationAddress = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentChildBirthDate
        {
            get { return CurrentChild?.BirthDate ?? DateTime.MinValue; }
            set
            {
                if (value == CurrentChild.BirthDate) return;
                CurrentChild.BirthDate = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public bool CurrentChildIsNobody
        {
            get { return CurrentChild != null && CurrentChild.IsNobody; }
            set
            {
                if (CurrentChild.IsNobody == value) return;
                CurrentChild.IsNobody = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public Sex CurrentChildSex
        {
            get { return CurrentChild?.Sex ?? Sex.Male; }
            set
            {
                if (CurrentChild.Sex == value) return;
                CurrentChild.Sex = value;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public Tarif CurrentChildTarif
        {
            get { return Tarifs?.First(t => t.Id == CurrentChild.TarifId); }
            set
            {
                if (CurrentChild.TarifId == value.Id) return;
                CurrentChild.TarifId = value.Id;
                _childNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentChildPeoplePhotoPath
        {
            get { return CurrentChild.Person.PhotoPath; }
            set
            {
                if (CurrentChild.Person.PhotoPath == value) return;
                CurrentChild.Person.PhotoPath = value;
                OnPropertyChanged(nameof(ChildImageSource));
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
            get { return CurrentFather?.Person?.LastName; }
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
            get { return CurrentFather?.Person?.FirstName; }
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
            get { return CurrentFather?.Person?.Patronymic; }
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
            get { return CurrentFather?.LocationAddress; }
            set
            {
                if (CurrentFather.LocationAddress == value) return;
                CurrentFather.LocationAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherResidenceAddress
        {
            get { return CurrentFather?.ResidenceAddress; }
            set
            {
                if (CurrentFather.ResidenceAddress == value) return;
                CurrentFather.ResidenceAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherWorkAddress
        {
            get { return CurrentFather?.WorkAddress; }
            set
            {
                if (CurrentFather.WorkAddress == value) return;
                CurrentFather.WorkAddress = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPhoneNumber
        {
            get { return CurrentFather?.PhoneNumber; }
            set
            {
                if (CurrentFather.PhoneNumber == value) return;
                CurrentFather.PhoneNumber = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPassportSeries
        {
            get { return CurrentFather?.PassportSeries; }
            set
            {
                if (CurrentFather.PassportSeries == value) return;
                CurrentFather.PassportSeries = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentFatherPassportIssuedBy
        {
            get { return CurrentFather?.PassportIssuedBy; }
            set
            {
                if (CurrentFather.PassportIssuedBy == value) return;
                CurrentFather.PassportIssuedBy = value;
                _fatherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentFatherPassportIssueDate
        {
            get { return CurrentFather?.PassportIssueDate ?? DateTime.MinValue; }
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
            get
            {
                return CurrentMother?.Person?.LastName;
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
            get { return CurrentMother?.Person?.FirstName; }
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
            get { return CurrentMother?.Person?.Patronymic; }
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
            get { return CurrentMother?.LocationAddress; }
            set
            {
                if (CurrentMother.LocationAddress == value) return;
                CurrentMother.LocationAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherResidenceAddress
        {
            get { return CurrentMother?.ResidenceAddress; }
            set
            {
                if (CurrentMother.ResidenceAddress == value) return;
                CurrentMother.ResidenceAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherWorkAddress
        {
            get { return CurrentMother?.WorkAddress; }
            set
            {
                if (CurrentMother.WorkAddress == value) return;
                CurrentMother.WorkAddress = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPhoneNumber
        {
            get { return CurrentMother?.PhoneNumber; }
            set
            {
                if (CurrentMother.PhoneNumber == value) return;
                CurrentMother.PhoneNumber = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPassportSeries
        {
            get { return CurrentMother?.PassportSeries; }
            set
            {
                if (CurrentMother.PassportSeries == value) return;
                CurrentMother.PassportSeries = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentMotherPassportIssuedBy
        {
            get { return CurrentMother?.PassportIssuedBy; }
            set
            {
                if (CurrentMother.PassportIssuedBy == value) return;
                CurrentMother.PassportIssuedBy = value;
                _motherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentMotherPassportIssueDate
        {
            get { return CurrentMother?.PassportIssueDate ?? DateTime.MinValue; }
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
            get { return CurrentOther?.Person?.LastName; }
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
            get { return CurrentOther?.Person?.FirstName; }
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
            get { return CurrentOther?.Person?.Patronymic; }
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
            get { return CurrentOther?.LocationAddress; }
            set
            {
                if (CurrentOther.LocationAddress == value) return;
                CurrentOther.LocationAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherResidenceAddress
        {
            get { return CurrentOther?.ResidenceAddress; }
            set
            {
                if (CurrentOther.ResidenceAddress == value) return;
                CurrentOther.ResidenceAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherWorkAddress
        {
            get { return CurrentOther?.WorkAddress; }
            set
            {
                if (CurrentOther.WorkAddress == value) return;
                CurrentOther.WorkAddress = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPhoneNumber
        {
            get { return CurrentOther?.PhoneNumber; }
            set
            {
                if (CurrentOther.PhoneNumber == value) return;
                CurrentOther.PhoneNumber = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPassportSeries
        {
            get { return CurrentOther?.PassportSeries; }
            set
            {
                if (CurrentOther.PassportSeries == value) return;
                CurrentOther.PassportSeries = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public string CurrentOtherPassportIssuedBy
        {
            get { return CurrentOther?.PassportIssuedBy; }
            set
            {
                if (CurrentOther.PassportIssuedBy == value) return;
                CurrentOther.PassportIssuedBy = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        public DateTime CurrentOtherPassportIssueDate
        {
            get { return CurrentOther?.PassportIssueDate ?? DateTime.MinValue; }
            set
            {
                if (CurrentOther.PassportIssueDate == value) return;
                CurrentOther.PassportIssueDate = value;
                _otherNotifier.OnPropertyChanged(value);
            }
        }

        #endregion

        public bool CurrentChildIsArchived => CurrentChild?.LastEnterChild?.ExpulsionDate.HasValue == true;

        public Group CurrentChildGroup
        {
            get { return _currentChildGroup; }
            set
            {
                if (Equals(value, _currentChildGroup)) return;
                _currentChildGroup = value;
                OnPropertyChanged();
            }
        }

        public string AnnualPaymentDescription => (string)this["annual_payment_description", "Родительский взнос за год"];

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

        public string MonthlyPaymentDescription => (string) this["monthly_payment_description", "Родительский взнос за месяц"];

        public string MonthlyPaymentMoney
        {
            get { return _monthlyPaymentMoney; }
            set
            {
                if (value == _monthlyPaymentMoney) return;
                _monthlyPaymentMoney = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ExpulsionDateLastEnterChild => CurrentChild?.LastEnterChild.ExpulsionDate;
        public string ExpulsionNoteLastEnterChild => CurrentChild?.LastEnterChild.ExpulsionNote;

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

        public DateTime StartDateOfNextAnnualPayment
        {
            get { return _startDateOfNextAnnualPayment; }
            set
            {
                if (value.Equals(_startDateOfNextAnnualPayment)) return;
                _startDateOfNextAnnualPayment = value;
                EndDateOfNextAnnualPayment = value.AddYears(1);
                OnPropertyChanged();
            }
        }

        public DateTime EndDateOfNextAnnualPayment
        {
            get { return _endDateOfNextAnnualPayment; }
            set
            {
                if (value.Equals(_endDateOfNextAnnualPayment)) return;
                _endDateOfNextAnnualPayment = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MonthlyPaymentsInYear> PaymentsInMonths
        {
            get { return _paymentsInMonths; }
            set
            {
                if (Equals(value, _paymentsInMonths)) return;
                _paymentsInMonths = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RangePayment> PaymentsInYears
        {
            get { return _paymentsInYears; }
            set
            {
                if (Equals(value, _paymentsInYears)) return;
                _paymentsInYears = value;
                OnPropertyChanged();
                StartDateOfNextAnnualPayment = GetLastAnnualPaymentDate(value);
            }
        }

        public DateTime RecalculationAnnualPaymentDate
        {
            get { return _recalculationAnnualPaymentDate; }
            set
            {
                if (value.Equals(_recalculationAnnualPaymentDate)) return;
                _recalculationAnnualPaymentDate = value;
                OnPropertyChanged();
            }
        }

        public MonthlyPayment LastMonthlyPayment
        {
            get { return _lastMonthlyPayment; }
            set
            {
                if (Equals(value, _lastMonthlyPayment)) return;
                _lastMonthlyPayment = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalChildUnpaidMonthCount));
                OnPropertyChanged(nameof(TotalChildUnpaidMoney));
                OnPropertyChanged(nameof(TotalChildDeposit));
            }
        }

        public double TotalChildUnpaidMonthCount
        {
            get
            {
                if (LastMonthlyPayment == null) return 0;
                var now = DateTime.Now;
                var paymentDate = LastMonthlyPayment.PaymentDate;
                return (now.Year - paymentDate.Year) * 12 + (now.Month - paymentDate.Month);
            }
        }

        public double TotalAnnualUnpaidMoney
        {
            get { return _totalAnnualUnpaidMoney; }
            set
            {
                if (value.Equals(_totalAnnualUnpaidMoney)) return;
                _totalAnnualUnpaidMoney = value;
                OnPropertyChanged();
            }
        }

        public ImageSource ChildImageSource
        {
            get { return _childImageSource; }
            private set
            {
                if (Equals(value, _childImageSource)) return;
                _childImageSource = value;
                OnPropertyChanged();
            }
        }

        private double GetTotalCredit()
        {
            if (LastMonthlyPayment == null) return 0d;
            return LastMonthlyPayment.DebtAfterPaying;
        }

        public double TotalChildUnpaidMoney => Math.Max(GetTotalCredit(), 0);
        public double TotalChildDeposit => Math.Max(-GetTotalCredit(), 0);

        public static IValueConverter GroupConverter =>
            _groupConverter ??
            (_groupConverter = (IValueConverter) Application.Current.FindResource("GroupsConverter"));

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
        private Group _currentChildGroup;
        private ObservableCollection<MonthlyPaymentsInYear> _paymentsInMonths;
        private KindergartenContext _paymentsContext;
        private MonthlyPayment _lastMonthlyPayment;
        private ObservableCollection<RangePayment> _paymentsInYears;
        private DateTime _endDateOfNextAnnualPayment;
        private DateTime _startDateOfNextAnnualPayment;
        private double _totalAnnualUnpaidMoney;
        private string _monthlyPaymentMoney;
        private Uri _imageUri;
        private ImageSource _childImageSource;
        private readonly OpenFileDialog _openFileDialog = IODialog.LoadOneImage;
        private bool _isNewPhoto;
        private string _oldPhotoSource;
        private volatile string _documentDirectoryPath;
        private DateTime _recalculationAnnualPaymentDate = DateTime.Today.AddDays(1);
        private static IValueConverter _groupConverter;
    }
}