using System;
using System.Windows;

namespace WpfApp.View
{
    /// <summary>
    /// Interaction logic for AddChildWindow.xaml
    /// </summary>
    public partial class AddChildWindow : Window
    {
        public static DateTime DefaultChildBirthDate { get; private set; } = DateTime.Now.AddYears(-3);

        public AddChildWindow()
        {
            InitializeComponent();
            Closed += (sender, args) =>
            {
//                if (DatePickerBirthDate.SelectedDate.HasValue)
//                    DefaultChildBirthDate = DatePickerBirthDate.SelectedDate.Value;
            };
        }
    }
}
