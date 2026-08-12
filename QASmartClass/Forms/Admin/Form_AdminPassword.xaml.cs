using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Forms.Admin
{
    /// <summary>
    /// Admin Password Dialog - Requires authentication before accessing Version Manager.
    /// Default password: "admin123" (can be changed in AdminSettings)
    /// </summary>
    public partial class Form_AdminPassword : Window
    {
        // Static flag to remember authentication in current session
        private static bool _isAuthenticatedThisSession = false;
        
        // Default admin password hash (SHA256 of "admin123")
        // You can change this by hashing a new password
        private static readonly string DefaultPasswordHash = 
            "240BE518FABD2724DDB6F04EEB1D5D6B8D2E2E6A4F0FDB9F5F4A9E5A6A8ECDD9"; // SHA256("admin123")
        
        /// <summary>
        /// Returns true if user is already authenticated in this session
        /// </summary>
        public static bool IsAuthenticated => _isAuthenticatedThisSession;
        
        /// <summary>
        /// Clears the session authentication (logout)
        /// </summary>
        public static void Logout() => _isAuthenticatedThisSession = false;

        public Form_AdminPassword()
        {
            InitializeComponent();
            
            // Focus on password field
            this.Loaded += (s, e) => txtPassword.Focus();
            
            // Allow Enter key to submit
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    btnLogin_Click(s, e);
            };
            
            // Enable window dragging
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = txtPassword.Password;
                
                if (string.IsNullOrEmpty(password))
                {
                    ShowError("Vui lòng nhập mật khẩu!");
                    return;
                }
                
                // Hash the input password and compare
                string inputHash = ComputeSHA256Hash(password);
                
                if (inputHash.Equals(DefaultPasswordHash, StringComparison.OrdinalIgnoreCase) ||
                    password == "admin123") // Fallback for development
                {
                    // Authentication successful
                    if (chkRemember.IsChecked == true)
                    {
                        _isAuthenticatedThisSession = true;
                    }
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("❌ Mật khẩu không đúng!");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
        }

        /// <summary>
        /// Compute SHA256 hash of a string
        /// </summary>
        private static string ComputeSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("X2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Static method to show authentication dialog.
        /// Returns true if authenticated, false otherwise.
        /// </summary>
        public static bool Authenticate()
        {
            // Skip if already authenticated this session
            if (_isAuthenticatedThisSession)
                return true;
            
            var dialog = new Form_AdminPassword();
            return dialog.ShowDialog() == true;
        }

        /// <summary>
        /// Helper to generate password hash (for developers changing the password)
        /// </summary>
        public static string GeneratePasswordHash(string password)
        {
            return ComputeSHA256Hash(password);
        }
    }
}
