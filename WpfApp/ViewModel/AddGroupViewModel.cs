using System.Threading.Tasks;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class AddGroupViewModel : ViewModelBase
    {
        public AddGroupViewModel()
        {
            AddGroupCommand = new RelayCommand<Group>(AddGroup);
        }

        public override void OnLoaded()
        {
            Pipe.SetParameter("added_group_result", null);
        }

        private async void AddGroup(Group group)
        {
            if (!group.IsValid()) return;

            AddGroupCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.Groups.Add(group);
                context.SaveChanges();
            });
            AddGroupCommand.NotifyCanExecute(true);
            Pipe.SetParameter("added_group_result", group);
            Finish();
        }
        
        public IRelayCommand AddGroupCommand { get; }
    }
}