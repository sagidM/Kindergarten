using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Camera
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IViewModel viewModel = new MainViewModel(Dispatcher);
            DataContext = viewModel;
            Closed += (s, e) => viewModel.OnClosed();
            viewModel.CloseRequire += (s, e) => Close();
        }

        private void ShowSettingsBorder_OnMouseEnter(object sender, MouseEventArgs e)
        {
            MenuStackPanel.IsEnabled = true;
            MenuStackPanel.Opacity = 1;
            ShowSettingsBorder.Visibility = Visibility.Hidden;
        }

        private int _inc;

        private void MenuStackPanel_OnMouseEnter(object sender, MouseEventArgs e)
        {
            _inc++;
        }

        private async void MenuStackPanel_OnMouseLeave(object sender, MouseEventArgs e)
        {
            var state = _inc;
            await Task.Run(() => Thread.Sleep(2000));
            if (state != _inc) return;
            _inc = 0;

            MenuStackPanel.IsEnabled = false;
            MenuStackPanel.Opacity = 0.1;
            ShowSettingsBorder.Visibility = Visibility.Visible;
        }
    }
}
