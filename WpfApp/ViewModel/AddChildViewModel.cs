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
        public AddChildViewModel()
        {
            AddChildCommand = new RelayCommand<Child>(AddChild);
            OpenDialogLoadImageCommand = new RelayCommand(OpenDialogLoadImage);
            CaptureFromCameraCommand = new RelayCommand(CaptureFromCamera);
        }

        private void CaptureFromCamera()
        {
            string path = @"C:\Users\123\Downloads\Camera Final\WpfCamera\bin\Debug\WpfCamera.exe";
            var process = new Process
            {
                StartInfo =
                {
                    FileName = path,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
            };
            process.Start();
            var imagePath = process.StandardOutput.ReadToEnd();
            imagePath = @"C:\Users\123\Downloads\Camera Final\WpfCamera\bin\Debug\photo.jpg";
            if (imagePath.Length > 0)
            {
                SetChildImage(imagePath);
            }
        }

        public override void OnLoaded()
        {
            Groups = (IEnumerable<Group>) Pipe.GetParameter("groups");
            Tarifs = (IEnumerable<Tarif>) Pipe.GetParameter("tarifs");
            Pipe.SetParameter("saved_result", false);
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

        private async void AddChild(Child child)
        {
            if (!child.IsValid()) return;

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
                    child.Tarif = null;
                    var enterChild = new EnterChild {Child = child};

                    var context = new KindergartenContext();
                    context.EnterChildren.Add(enterChild);
                    context.SaveChanges();
                }
                catch
                {
                    if (imageFileName != null)
                        File.Delete(imageFileName);
                    throw;
                }
            });
            AddChildCommand.NotifyCanExecute(true);
            Pipe.SetParameter("saved_result", true);
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

        public IRelayCommand OpenDialogLoadImageCommand { get; }
        public IRelayCommand AddChildCommand { get; }
        public IRelayCommand CaptureFromCameraCommand { get; }

        private readonly OpenFileDialog _openFileDialog = FileDialogs.LoadOneImage;
        private ImageSource _childImageSource;
        private Uri _imageUri;
        private IEnumerable<Tarif> _tarifs;
        private IEnumerable<Group> _groups;
    }
}