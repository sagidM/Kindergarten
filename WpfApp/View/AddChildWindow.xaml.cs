using System;
using System.Windows;
using WpfApp.ViewModel;

namespace WpfApp.View
{
    /// <summary>
    /// Interaction logic for AddChildWindow.xaml
    /// </summary>
    public partial class AddChildWindow : Window
    {
        public static DateTime DefaultChildBirthDate { get; private set; }= DateTime.Now.AddYears(-3);

        public AddChildWindow()
        {
            InitializeComponent();
            ((ICloseableViewModel) DataContext).ClosingRequest += (sender, args) => Close();
            Closed += (sender, args) =>
            {
                if (DatePickerBirthDate.SelectedDate.HasValue)
                    DefaultChildBirthDate = DatePickerBirthDate.SelectedDate.Value;
            };
        }
    }
}
