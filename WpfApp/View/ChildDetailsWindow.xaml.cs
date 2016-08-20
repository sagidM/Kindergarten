using System.Windows;
using System.Windows.Input;

namespace WpfApp.View
{
    /// <summary>
    /// Interaction logic for ChildDetailsWindow.xaml
    /// </summary>
    public partial class ChildDetailsWindow : Window
    {
        public ChildDetailsWindow()
        {
            InitializeComponent();
            PreviewKeyDown += (o, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        }
    }
}
