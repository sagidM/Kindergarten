using System;
using System.Collections.Generic;
using System.Configuration;
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
        private static readonly string WebcamPath = ConfigurationManager.AppSettings["WebcamPath"];
        private const string WebcamArguments = "photo 1";

        public IRelayCommand OpenDialogLoadImageCommand { get; }
        public IRelayCommand AddChildCommand { get; }
        public IRelayCommand CaptureFromCameraCommand { get; }
        public IRelayCommand OpenImageChosenCommand { get; }
        public IRelayCommand ChooseParentCommand { get; }
        public IRelayCommand DetachParentCommand { get; }

        public AddChildViewModel()
        {
            AddChildCommand = new RelayCommand<Child>(AddChild);
            OpenDialogLoadImageCommand = new RelayCommand(OpenDialogLoadImage);
            CaptureFromCameraCommand = new RelayCommand(CaptureFromCamera);
            OpenImageChosenCommand = new RelayCommand(OpenChosenImage);
            ChooseParentCommand = new RelayCommand<Parents>(ChooseParent);
            DetachParentCommand = new RelayCommand<Parents>(DetachParent);
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
            string text = null;
            if (parentType == Parents.Other)
            {
                text = IODialog.InputDialog("Кем приходится ребёнку", "Иной представитель", OtherParentText);
                if (text == null)
                    return;
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

        private void OpenChosenImage()
        {
            if (_imageUri != null)
                Process.Start(_imageUri.AbsolutePath);
        }

        private void CaptureFromCamera()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = WebcamPath, RedirectStandardOutput = true, UseShellExecute = false, Arguments = WebcamArguments,
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
            Tarifs = (IEnumerable<Tarif>) Pipe.GetParameter("tarifs");
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

            AddChildCommand.NotifyCanExecute(false);
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
                    var enterChild = new EnterChild { Child = child, EnterDate = ChildAdditionDate };
                    child.LastEnterChild = enterChild;

                    var context = new KindergartenContext();

                    if (StartedToPayFromAdditionDate)
                    {
                        // to no debt range add

                        // 1) the first fictitious payment for start range
                        context.MonthlyPayments.Add(new MonthlyPayment
                        {
                            ChildId = child.Id,
                            PaymentDate = ChildAdditionDate,
                            MoneyPaymentByTarif = 0, // 0 - because to make no debt
                        });

                        // 2) the second fictitious payment for end range
                        context.MonthlyPayments.Add(new MonthlyPayment
                        {
                            ChildId = child.Id,
                            PaymentDate = DateTime.Now,
                            MoneyPaymentByTarif = child.Tarif.MonthlyPayment,
                            DebtAfterPaying = child.Tarif.MonthlyPayment,
                        });
                    }
                    child.Tarif = null;

                    context.EnterChildren.Add(enterChild);

                    if (SelectedFather != null)
                        context.ParentChildren.Add(new ParentChild { Child = child, ParentId = SelectedFather.Id, ParentType = Parents.Father});
                    if (SelectedMother != null)
                        context.ParentChildren.Add(new ParentChild { Child = child, ParentId = SelectedMother.Id, ParentType = Parents.Mother});
                    if (SelectedOther != null)
                        context.ParentChildren.Add(new ParentChild { Child = child, ParentId = SelectedOther.Id, ParentType = Parents.Other, ParentTypeText = OtherParentText });
                    context.SaveChanges();
                }
                catch
                {
                    if (imageFileName != null)
                        File.Delete(imageFileName);
                    throw;
                }
            });
            Pipe.SetParameter("saved_child_result", child);
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


        private void OpenDialogLoadImage()
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
        private IEnumerable<Tarif> _tarifs;
        private IEnumerable<Group> _groups;
        private Parent _selectedFather;
        private Parent _selectedOther;
        private Parent _selectedMother;

        private Sex _otherSex;
        private string _otherParentText;
        private bool _startedToPayFromAdditionDate;
        private DateTime _childAdditionDate = DateTime.Now;
    }
}