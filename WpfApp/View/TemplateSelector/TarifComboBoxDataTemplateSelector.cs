using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DAL.Model;
// ReSharper disable PossibleNullReferenceException

namespace WpfApp.View.TemplateSelector
{
    public class TarifComboBoxDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectedItemMonthlyTemplate { get; set; }
        public DataTemplate SelectedItemAnnualTemplate { get; set; }
        public DataTemplate MonthlyTemplate { get; set; }
        public DataTemplate AnnualTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var tarif = (Tarif)item;

            var parent = container;
            while (!(parent is ComboBoxItem) && !(parent is ComboBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is ComboBox)
            {
                // selected
                return tarif.AnnualPayment > 0 ? SelectedItemAnnualTemplate : SelectedItemMonthlyTemplate;
            }
            return tarif.AnnualPayment > 0 ? AnnualTemplate : MonthlyTemplate;
        }
    }
}