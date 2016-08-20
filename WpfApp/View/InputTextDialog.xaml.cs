using System.Windows;

namespace WpfApp.View
{
    /// <summary>
    /// Interaction logic for InputTextDialog.xaml
    /// </summary>
    public partial class InputTextDialog : Window
    {
        public InputTextDialog()
        {
            InitializeComponent();
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
