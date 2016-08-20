using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;
using WpfApp.Util;

namespace WpfApp.ViewModel
{
    public class AddParentViewModel : ViewModelBase
    {
        public IRelayCommand SaveParentCommand { get; }
        public IRelayCommand ChooseParentCommand { get; }

        public AddParentViewModel()
        {
            SaveParentCommand = new RelayCommand<Parent>(SaveParent);
            ChooseParentCommand = new RelayCommand<Parent>(ChooseParent);
        }

        private void ChooseParent(Parent parent)
        {
            Pipe.SetParameter("parent_result", parent);
            Finish();
        }

        private void SaveParent(Parent parent)
        {
            var context = new KindergartenContext();
            context.Parents.Add(parent);

            try
            {
                context.SaveChanges();
                Pipe.SetParameter("parent_result", parent);
                Finish();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка");
            }
        }

        public override void OnLoaded()
        {
            Pipe.SetParameter("parent_result", null);
            UpdateParents();
        }

        private void UpdateParents()
        {
            var context = new KindergartenContext();
            Parents = new ListCollectionView(context.Parents.Include("Person").ToList());
        }

        public ListCollectionView Parents
        {
            get { return _parents; }
            set
            {
                if (Equals(value, _parents)) return;
                _parents = value;
                if (_parents != null)
                {
                    _parents.Filter = o =>
                    {
                        var p = (Parent)o;
                        if (!string.IsNullOrEmpty(SearchParentFilter))
                        {
                            if (p.Person.FullName.IndexOf(SearchParentFilter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                                return true;
                            if (p.Id.ToString() == SearchParentFilter)
                                return true;
                            return false;
                        }
                        return true;
                    };
                }
                OnPropertyChanged();
            }
        }

        public string SearchParentFilter
        {
            get { return _searchParentFilter; }
            set
            {
                if (value == _searchParentFilter) return;
                _searchParentFilter = value;
                Parents.TryRefreshFilter();
                OnPropertyChanged();
            }
        }

        public BitmapImage ParentImageSource
        {
            get { return _parentImageSource; }
            set
            {
                if (Equals(value, _parentImageSource)) return;
                _parentImageSource = value;
                OnPropertyChanged();
            }
        }
        
        private ListCollectionView _parents;
        private BitmapImage _parentImageSource;
        private string _searchParentFilter;
    }
}