using System.Threading.Tasks;
using DAL.Model;
using WpfApp.Framework.Command;
using WpfApp.Framework.Core;

namespace WpfApp.ViewModel
{
    public class AddTarifViewModel : ViewModelBase
    {
        public IRelayCommand AddTarifCommand { get; }
        public AddTarifViewModel()
        {
            AddTarifCommand = new RelayCommand<Tarif>(AddTarif);
        }

        private async void AddTarif(Tarif tarif)
        {
            if (!tarif.IsValid()) return;

            AddTarifCommand.NotifyCanExecute(false);
            await Task.Run(() =>
            {
                var context = new KindergartenContext();
                context.Tarifs.Add(tarif);
                context.SaveChanges();
            });
            AddTarifCommand.NotifyCanExecute(true);

            Pipe.SetParameter("tarif_result", tarif);
            Finish();
        }

        public override void OnLoaded()
        {
            Pipe.SetParameter("tarif_result", null);
        }
    }
}