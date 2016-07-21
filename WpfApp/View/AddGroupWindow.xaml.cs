using System.Windows;
using WpfApp.ViewModel;

namespace WpfApp.View
{
    /// <summary>
    /// Interaction logic for AddGroupWindow.xaml
    /// </summary>
    public partial class AddGroupWindow : Window
    {
        public AddGroupWindow()
        {
            InitializeComponent();
            ((ICloseableViewModel)DataContext).ClosingRequest += (sender, args) => Close();
        }
    }
}
