using System;
using System.Collections.ObjectModel;
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
    internal class AddChildViewModel : ViewModelBase
    {
        private readonly KindergartenContext _context;
        public AddChildViewModel()
        {
            AddChildCommand = new RelayCommand<Child>(AddChild);
            OpenDialogLoadImageCommand = new RelayCommand(OpenDialogLoadImage);
            if (IsDesignerMode) return;

            _context = new KindergartenContext();
            Load();
        }

        private async void Load()
        {
            Groups = await Task.Run(() => new ObservableCollection<Group>(_context.Groups));
        }

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

        public IRelayCommand OpenDialogLoadImageCommand { get; }
        public IRelayCommand AddChildCommand { get; }


        private async void AddChild(Child child)
        {
            if (!child.IsValid()) return;

            AddChildCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                string fileName = null;
                try
                {
                    if (_imageUri != null)
                    {
                        var savePath = Path.Combine(Settings.AppFilePaths.ChildImages, CommonHelper.GetUniqueString());
                        fileName = ImageUtil.SaveImage(_imageUri, savePath);
                        child.Person.PhotoPath = Path.GetFileName(fileName);
                    }
                    _context.Children.Add(child);
                    _context.SaveChanges();
                }
                catch
                {
                    if (fileName != null)
                        File.Delete(fileName);
                    throw;
                }
            });
            AddChildCommand.NotifyCanExecute(true);
            Finish();
        }

        private readonly IFileDialogService _openFileDialogService = FileDialogServices.ImageLoader;
        private ImageSource _childImageSource;
        private Uri _imageUri;

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
            if (_openFileDialogService.Show() == false) return;

            var path = _openFileDialogService.FileName;
            _imageUri = new Uri(path);
            try
            {
                ChildImageSource = new BitmapImage(_imageUri);
            }
            catch
            {
                _imageUri = null;
                MessageBox.Show("Изображение не поддерживается", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}