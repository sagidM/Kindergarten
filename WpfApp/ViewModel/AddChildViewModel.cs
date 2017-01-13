using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DAL.Model;
using Microsoft.Win32;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.Util;
using WpfApp.View.DialogService;

namespace WpfApp.ViewModel
{
    internal class AddChildViewModel : ViewModelBase
    {
        public IRelayCommand LoadImageFromFileCommand { get; }
        public IRelayCommand AddChildCommand { get; }
        public IRelayCommand CaptureImageFromCameraCommand { get; }
        public IRelayCommand OpenImageChoosingCommand { get; }
        public IRelayCommand RemoveImageCommand { get; }
        public IRelayCommand ChooseParentCommand { get; }
        public IRelayCommand DetachParentCommand { get; }

        public AddChildViewModel()
        {
            AddChildCommand = new RelayCommand<Child>(AddChild);
            LoadImageFromFileCommand = new RelayCommand(LoadImageFromFile);
            CaptureImageFromCameraCommand = new RelayCommand(CaptureImageFromCamera);
            OpenImageChoosingCommand = new RelayCommand(OpenImageChoosing);
            RemoveImageCommand = new RelayCommand(RemoveImage);
            ChooseParentCommand = new RelayCommand<Parents>(ChooseParent);
            DetachParentCommand = new RelayCommand<Parents>(DetachParent);
        }

        private void RemoveImage()
        {
            _imageUri = null;
            ChildImageSource = null;
        }

        private void DetachParent(Parents parents)
        {
            switch (parents)
            {
                case Parents.Father:
                    SelectedFather = null;
                    break;
                case Parents.Mother:
                    SelectedMother = null;
                    break;
                case Parents.Other:
                    OtherParentText = null;
                    SelectedOther = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parents), parents, null);
            }
        }

        private void ChooseParent(Parents parentType)
        {
            var text = OtherParentText;
            if (parentType == Parents.Other)
            {
                do
                {
                    text = IODialog.InputDialog("Кем приходится ребёнку", "Иной представитель", text);
                    if (text == null)
                        return;
                } while (text.Trim().Length == 0);
            }

            var pipe = new Pipe(true);

            var exclude = new List<int>(3);
            if (SelectedFather != null) exclude.Add(SelectedFather.Id);
            if (SelectedMother != null) exclude.Add(SelectedMother.Id);
            if (SelectedOther != null) exclude.Add(SelectedOther.Id);
            pipe.SetParameter("exclude_parent_ids", exclude.ToArray());

            StartViewModel<AddParentViewModel>(pipe);
            var parent = (Parent) pipe.GetParameter("parent_result");
            if (parent == null) return;
            switch (parentType)
            {
                case Parents.Father:
                    SelectedFather = parent;
                    break;
                case Parents.Mother:
                    SelectedMother = parent;
                    break;
                case Parents.Other:
                    OtherParentText = text;
                    SelectedOther = parent;
                    break;
            }
        }

        private void OpenImageChoosing()
        {
            if (_imageUri != null)
                CommonHelper.OpenFileOrDirectoryWithSelected(Path.GetFullPath(_imageUri.AbsolutePath));
        }

        private void CaptureImageFromCamera()
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
                SetChildImage(imagePath);
            }
        }

        public override void OnLoaded()
        {
            Groups = (IEnumerable<Group>) Pipe.GetParameter("groups");
            Tarifs = (IList<Tarif>) Pipe.GetParameter("tarifs");
            Pipe.SetParameter("saved_child_result", null);
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

        public IList<Tarif> Tarifs
        {
            get { return _tarifs; }
            set
            {
                if (Equals(value, _tarifs)) return;
                _tarifs = value;
                OnPropertyChanged();
            }
        }

        public Parent SelectedFather
        {
            get { return _selectedFather; }
            set
            {
                if (Equals(value, _selectedFather)) return;
                _selectedFather = value;
                OnPropertyChanged();
            }
        }

        public Parent SelectedMother
        {
            get { return _selectedMother; }
            set
            {
                if (Equals(value, _selectedMother)) return;
                _selectedMother = value;
                OnPropertyChanged();
            }
        }

        public Parent SelectedOther
        {
            get { return _selectedOther; }
            set
            {
                if (Equals(value, _selectedOther)) return;
                _selectedOther = value;
                OnPropertyChanged();
            }
        }

        public Sex OtherSex
        {
            get { return _otherSex; }
            set
            {
                if (value == _otherSex) return;
                _otherSex = value;
                OnPropertyChanged();
            }
        }

        private async void AddChild(Child child)
        {
            if (!child.IsValid())
            {
                MessageBox.Show("Не все поля заполнены/пункты выбраны", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (child.BirthDate > ChildAdditionDate)
            {
                MessageBox.Show("Дата рождения должна быть меньше даты рождения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedOther == null && SelectedMother == null && SelectedFather == null)
            {
                MessageBox.Show("Должен быть выбран хотя бы один из родителей либо иной представитель", "Некорректный выбор", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (BroughtParent == Parents.Father && SelectedFather == null ||
                BroughtParent == Parents.Mother && SelectedMother == null ||
                BroughtParent == Parents.Other && SelectedOther == null)
            {
                MessageBox.Show("Выберите существующего родителя");
                return;
            }
            AddChildCommand.NotifyCanExecute(false);

            ParentChild broghtParent = null;
            EnterChild enterChild = null;
            bool needAddPayments = StartedToPayFromAdditionDate;
            await Task.Run(() =>
            {
                string imageFileName = null;
                try
                {
                    if (_imageUri != null)
                    {
                        var savePath = Path.Combine(Settings.AppFilePaths.ChildImages, CommonHelper.GetUniqueString());
                        imageFileName = ImageUtil.SaveImage(_imageUri, savePath);
                        child.Person.PhotoPath = Path.GetFileName(imageFileName);
                    }

                    // groups and tarifs (in OnLoaded) are from other context
                    child.GroupId = child.Group.Id;
                    child.Group = null;
                    child.TarifId = child.Tarif.Id;
                    enterChild = new EnterChild {Child = child, EnterDate = ChildAdditionDate};
                    child.LastEnterChild = enterChild;

                    var context = new KindergartenContext();

                    if (needAddPayments)
                    {
                        // to no debt range add

                        var now = DateTime.Now;

                        if (now.Year != ChildAdditionDate.Year && now.Month != ChildAdditionDate.Month)
                        {
                            // 1) the first fictitious payment for start range
                            context.MonthlyPayments.Add(new MonthlyPayment
                            {
                                ChildId = child.Id,
                                PaymentDate = ChildAdditionDate,
                                MoneyPaymentByTarif = 0, // 0 - because to make no debt
                            });
                        }

                        // 2) the second fictitious payment for end range
                        context.MonthlyPayments.Add(new MonthlyPayment
                        {
                            ChildId = child.Id, PaymentDate = now, MoneyPaymentByTarif = child.Tarif.MonthlyPayment, DebtAfterPaying = InitialDebt,
                        });
                    }
                    child.Tarif = null;

                    context.EnterChildren.Add(enterChild);

                    if (SelectedFather != null)
                    {
                        var parentChild = new ParentChild {Child = child, ParentId = SelectedFather.Id, ParentType = Parents.Father};
                        context.ParentChildren.Add(parentChild);
                        if (BroughtParent == Parents.Father)
                            broghtParent = parentChild;
                    }
                    if (SelectedMother != null)
                    {
                        var parentChild = new ParentChild {Child = child, ParentId = SelectedMother.Id, ParentType = Parents.Mother};
                        context.ParentChildren.Add(parentChild);
                        if (BroughtParent == Parents.Mother)
                            broghtParent = parentChild;
                    }
                    if (SelectedOther != null)
                    {
                        var parentChild = new ParentChild {Child = child, ParentId = SelectedOther.Id, ParentType = Parents.Other, ParentTypeText = OtherParentText};
                        context.ParentChildren.Add(parentChild);
                        if (BroughtParent == Parents.Other)
                            broghtParent = parentChild;
                    }
                    context.SaveChanges();
                    App.Logger.Debug("Child added");
                }
                catch
                {
                    if (imageFileName != null)
                        File.Delete(imageFileName);
                    throw;
                }
            });
            Pipe.SetParameter("saved_child_result", child);
            Pipe.SetParameter("brought_parent", broghtParent);
            Pipe.SetParameter("enter", enterChild);
            // ReSharper disable once RedundantExplicitArraySize
            var parents = new ParentChild[3]
            {
                new ParentChild {Parent= SelectedFather, ParentType = Parents.Father},
                new ParentChild {Parent=SelectedMother, ParentType=Parents.Mother},
                new ParentChild {Parent = SelectedOther, ParentType = Parents.Other}
            };
            Pipe.SetParameter("parents", parents);
            AddChildCommand.NotifyCanExecute(true);
            Finish();
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

        public string OtherParentText
        {
            get { return _otherParentText; }
            set
            {
                if (value == _otherParentText) return;
                _otherParentText = value;
                OnPropertyChanged();
            }
        }

        public bool StartedToPayFromAdditionDate
        {
            get { return _startedToPayFromAdditionDate; }
            set
            {
                if (value == _startedToPayFromAdditionDate) return;
                _startedToPayFromAdditionDate = value;
                if (!value && _selectedTarifIndex >= 0) InitialDebt = Tarifs[_selectedTarifIndex].MonthlyPayment;
                OnPropertyChanged();
            }
        }

        public DateTime ChildAdditionDate
        {
            get { return _childAdditionDate; }
            set
            {
                if (value.Equals(_childAdditionDate)) return;
                _childAdditionDate = value;
                OnPropertyChanged();
            }
        }

        public Parents BroughtParent
        {
            get { return _broughtParent; }
            set
            {
                if (Equals(value, _broughtParent)) return;
                _broughtParent = value;
                OnPropertyChanged();
            }
        }

        public double InitialDebt
        {
            get { return Math.Max(0, _initialDebtAndCredit); }
            set
            {
                if (value.Equals(_initialDebtAndCredit)) return;
                _initialDebtAndCredit = value;
                OnPropertyChanged(nameof(InitialCredit));
                OnPropertyChanged();
            }
        }

        public double InitialCredit
        {
            get { return Math.Max(0, -_initialDebtAndCredit); }
            set
            {
                value = -value;
                if (value.Equals(_initialDebtAndCredit)) return;
                _initialDebtAndCredit = value;
                OnPropertyChanged(nameof(InitialDebt));
                OnPropertyChanged();
            }
        }
        private double _initialDebtAndCredit;

        public int SelectedTarifIndex
        {
            set
            {
                _selectedTarifIndex = value;
                if (!StartedToPayFromAdditionDate)
                    InitialDebt = Tarifs[value].MonthlyPayment;
            }
        }


        // copypaste in ChildDetailsViewModel.cs
        private void LoadImageFromFile()
        {
            if (_openFileDialog.ShowDialog() == false) return;

            var path = _openFileDialog.FileName;
            SetChildImage(path);
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
            catch
            {
                _imageUri = null;
                MessageBox.Show("Изображение не поддерживается", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private readonly OpenFileDialog _openFileDialog = IODialog.LoadOneImage;
        private ImageSource _childImageSource;
        private Uri _imageUri;
        private IList<Tarif> _tarifs;
        private IEnumerable<Group> _groups;
        private Parent _selectedFather;
        private Parent _selectedOther;
        private Parent _selectedMother;

        private Sex _otherSex;
        private string _otherParentText;
        private bool _startedToPayFromAdditionDate;
        private DateTime _childAdditionDate = DateTime.Now;
        private Parents _broughtParent;
        private int _selectedTarifIndex = -1;
    }
}