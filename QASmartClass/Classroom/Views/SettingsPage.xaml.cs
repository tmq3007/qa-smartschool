using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.ViewModels;

namespace QASmartClass.Classroom.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            var vm = new SettingsViewModel();
            DataContext = vm;
            Loaded += (_, _) => {
                vm.LoadSettings();
                CheckAdminLock();
            };

            // Đồng bộ an toàn PasswordBox với ViewModel
            txtCurrentPassword.PasswordChanged += (s, e) => vm.CurrentPassword = txtCurrentPassword.Password;
            txtNewPassword.PasswordChanged += (s, e) => vm.NewPassword = txtNewPassword.Password;
            txtConfirmPassword.PasswordChanged += (s, e) => vm.ConfirmPassword = txtConfirmPassword.Password;
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }


        private void Menu_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                UpdateMenuSelection(b);

                var t = tag switch {
                    "role" => sectionRole,
                    "general" => sectionGeneral,
                    "media" => sectionMedia,
                    "network" => sectionNetwork,
                    "storage" => sectionStorage,
                    "account" => sectionAccount,
                    "about" => sectionAbout,
                    "api" => sectionApi,
                    "grade" => sectionGrade,
                    "school" => sectionSchool,
                    _ => null
                };
                t?.BringIntoView();
            }
        }

        private void UpdateMenuSelection(Border activeBorder)
        {
            var menuBorders = new[] { menuRole, menuGeneral, menuMedia, menuNetwork, menuStorage, menuAccount, menuAbout, menuApi, menuGrade, menuSchool };
            
            var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
            var activeTextBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
            var inactiveTextBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#495057"));

            foreach (var border in menuBorders)
            {
                if (border == null) continue;

                if (border == activeBorder)
                {
                    border.Background = activeBrush;
                    if (border.Child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[1] is TextBlock tb)
                    {
                        tb.Foreground = activeTextBrush;
                        tb.FontWeight = FontWeights.SemiBold;
                    }
                }
                else
                {
                    border.Background = Brushes.Transparent;
                    if (border.Child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[1] is TextBlock tb)
                    {
                        tb.Foreground = inactiveTextBrush;
                        tb.FontWeight = FontWeights.Normal;
                    }
                }
            }
        }
        
        private void OpenStudentClient_Click(object sender, MouseButtonEventArgs e) => ((SettingsViewModel)DataContext).OpenStudentClientCommand.Execute(null);

        private void CheckAdminLock()
        {
        }
    }
}
