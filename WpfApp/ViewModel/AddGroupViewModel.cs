using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DAL.Model;
using WpfApp.Command;
using WpfApp.Framework.Core;
using WpfApp.Service;
using WpfApp.Util;

namespace WpfApp.ViewModel
{
    public class AddGroupViewModel : ViewModelBase
    {

        public AddGroupViewModel()
        {
            AddGroupCommand = new RelayCommand<Group>(AddGroup);
            OpenDialogLoadImageCommand = new RelayCommand(OpenDialogLoadImage);
        }

        private readonly IFileDialogService _openFileDialogService = FileDialogServices.ImageLoader;
        private ImageSource _groupImageSource;
        private Uri _imageUri;
        private void OpenDialogLoadImage()
        {
            if (_openFileDialogService.Show() == false) return;

            var path = _openFileDialogService.FileName;
            _imageUri = new Uri(path);
            try
            {
                GroupImageSource = new BitmapImage(_imageUri);
            }
            catch
            {
                _imageUri = null;
                MessageBox.Show("Изображение не поддерживается", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public ImageSource GroupImageSource
        {
            get { return _groupImageSource; }
            private set
            {
                if (Equals(value, _groupImageSource)) return;
                _groupImageSource = value;
                OnPropertyChanged();
            }
        }

        private async void AddGroup(Group group)
        {
            if (!group.IsValid()) return;

            AddGroupCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                string fileName = null;
                try
                {
                    if (_imageUri != null)
                    {
                        var savePath = Path.Combine(Settings.AppFilePaths.GroupImages, CommonHelper.GetUniqueString());
                        fileName = ImageUtil.SaveImage(_imageUri, savePath);
                        group.PhotoPath = Path.GetFileName(fileName);
                    }
                    var context = new KindergartenContext();
                    context.Groups.Add(group);
                    context.SaveChanges();
                }
                catch
                {
                    if (fileName != null)
                        File.Delete(fileName);
                    throw;
                }
            });
            AddGroupCommand.NotifyCanExecute(true);
            Finish();
        }

        public IRelayCommand OpenDialogLoadImageCommand { get; }
        public IRelayCommand AddGroupCommand { get; }
    }
}