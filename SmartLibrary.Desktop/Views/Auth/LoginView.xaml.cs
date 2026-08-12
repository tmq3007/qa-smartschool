using System.Windows;
using System.Windows.Controls;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Auth
{
    public partial class LoginView : UserControl
    {
        private System.Windows.Threading.DispatcherTimer? _passwordHideTimer;

        public LoginView()
        {
            InitializeComponent();
            this.DataContextChanged += LoginView_DataContextChanged;
            this.Unloaded += LoginView_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
        }

        private void LoginView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_passwordHideTimer != null)
            {
                _passwordHideTimer.Stop();
                _passwordHideTimer.Tick -= PasswordHideTimer_Tick;
                _passwordHideTimer = null;
            }

            if (DataContext is LoginViewModel vm)
            {
                vm.PropertyChanged -= ViewModel_PropertyChanged;
            }
        }

        private void InitializePasswordTimer()
        {
            if (_passwordHideTimer == null)
            {
                _passwordHideTimer = new System.Windows.Threading.DispatcherTimer();
                _passwordHideTimer.Interval = System.TimeSpan.FromSeconds(10);
                _passwordHideTimer.Tick += PasswordHideTimer_Tick;
            }
        }

        private void PasswordHideTimer_Tick(object? sender, System.EventArgs e)
        {
            _passwordHideTimer?.Stop();
            if (DataContext is LoginViewModel vm)
            {
                vm.IsPasswordVisible = false;
            }
        }

        private void LoginView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
                txtPassword.Password = newVm.PasswordText;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.IsPasswordVisible))
            {
                if (DataContext is LoginViewModel vm)
                {
                    if (vm.IsPasswordVisible)
                    {
                        InitializePasswordTimer();
                        _passwordHideTimer?.Stop();
                        _passwordHideTimer?.Start();
                    }
                    else
                    {
                        _passwordHideTimer?.Stop();
                    }

                    if (txtPassword.Password != vm.PasswordText)
                    {
                        txtPassword.Password = vm.PasswordText;
                    }
                }
            }
            else if (e.PropertyName == nameof(LoginViewModel.PasswordText))
            {
                if (DataContext is LoginViewModel vm)
                {
                    if (vm.IsPasswordVisible)
                    {
                        // Reset timer on keystroke
                        _passwordHideTimer?.Stop();
                        _passwordHideTimer?.Start();
                    }

                    if (txtPassword.Password != vm.PasswordText)
                    {
                        txtPassword.Password = vm.PasswordText;
                    }
                }
            }
            else if (e.PropertyName == nameof(LoginViewModel.ErrorMessage))
            {
                if (DataContext is LoginViewModel vm && !string.IsNullOrEmpty(vm.ErrorMessage))
                {
                    var sb = Resources["ShakeAnimation"] as System.Windows.Media.Animation.Storyboard;
                    sb?.Begin(this);
                }
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                if (vm.PasswordText != txtPassword.Password)
                {
                    vm.PasswordText = txtPassword.Password;
                }
            }
        }
    }
}
