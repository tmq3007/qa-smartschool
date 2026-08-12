using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Lớp hỗ trợ thiết lập Low-level Keyboard Hook bằng Win32 API
    /// để chặn các phím tắt hệ thống như Alt+Tab, Alt+F4, Windows Key.
    /// </summary>
    public static class KeyboardHookHelper
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vId);

        private static LowLevelKeyboardProc? _proc;
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool _isEnabled = false;

        // === UPGRADE_01: Expose trạng thái hook cho Kiosk Timer kiểm tra ===
        public static bool IsEnabled => _isEnabled && _hookID != IntPtr.Zero;

        // === UPGRADE_11: OTP-based emergency bypass (thay thế SHA-256 hardcoded) ===
        public static string EmergencyOTP { get; set; } = string.Empty;
        private static int _failedBypassAttempts = 0;
        private static DateTime _bypassLockoutUntil = DateTime.MinValue;
        private const int MAX_BYPASS_ATTEMPTS = 5;
        private const int LOCKOUT_MINUTES = 5;

        public static void EnableHook()
        {
            if (_isEnabled) return;
            try
            {
                _proc = HookCallback;
                _hookID = SetHook(_proc);

                // === UPGRADE_01: Kiểm tra hook thực sự được cài đặt thành công ===
                if (_hookID == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Log.Error("[KeyboardHook] SetWindowsHookEx FAILED! Win32 Error Code: {Code}", errorCode);
                    _isEnabled = false;
                    return;
                }

                _isEnabled = true;
                Log.Information("[KeyboardHook] Low-level keyboard hook enabled successfully. HookID: {ID}", _hookID);
            }
            catch (Exception ex)
            {
                Log.Warning("[KeyboardHook] Failed to enable keyboard hook: {Err}", ex.Message);
            }
        }

        public static void DisableHook()
        {
            if (!_isEnabled) return;
            try
            {
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                }
                _proc = null;
                _isEnabled = false;
                Log.Information("[KeyboardHook] Low-level keyboard hook disabled.");
            }
            catch (Exception ex)
            {
                Log.Warning("[KeyboardHook] Failed to disable keyboard hook: {Err}", ex.Message);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Kiểm tra trạng thái phím Alt, Ctrl, Shift từ GetAsyncKeyState
                // VK_MENU (Alt) = 0x12, VK_CONTROL (Ctrl) = 0x11, VK_SHIFT (Shift) = 0x10
                bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;

                // 1. Chặn phím Windows Trái/Phải (mã 91, 92)
                if (vkCode == 91 || vkCode == 92)
                {
                    Log.Information("[KeyboardHook] Blocked Windows key press.");
                    return new IntPtr(1);
                }

                // 2. Chặn Alt + Tab (Tab = 9)
                if (vkCode == 9 && alt)
                {
                    Log.Information("[KeyboardHook] Blocked Alt + Tab.");
                    return new IntPtr(1);
                }

                // 3. Chặn Alt + F4 (F4 = 115)
                if (vkCode == 115 && alt)
                {
                    Log.Information("[KeyboardHook] Blocked Alt + F4.");
                    return new IntPtr(1);
                }

                // 4. Chặn Alt + Esc hoặc Ctrl + Esc (Esc = 27)
                if (vkCode == 27 && (alt || ctrl))
                {
                    Log.Information("[KeyboardHook] Blocked ESC system shortcut.");
                    return new IntPtr(1);
                }

                // 5. Chặn Win + Tab (Tab = 9, LWin/RWin đang nhấn)
                bool winPressed = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
                if (vkCode == 9 && winPressed)
                {
                    Log.Information("[KeyboardHook] Blocked Win + Tab Virtual Desktop Switch.");
                    return new IntPtr(1);
                }

                // 6. Chặn Win + Ctrl + D/F4/Left/Right (D = 68, F4 = 115, Left = 37, Right = 39)
                if (winPressed && ctrl)
                {
                    if (vkCode == 68 || vkCode == 115 || vkCode == 37 || vkCode == 39)
                    {
                        Log.Information("[KeyboardHook] Blocked Virtual Desktop Shortcut: VK {Code}", vkCode);
                        return new IntPtr(1);
                    }
                }

                // 7. Chặn các phím tắt trình duyệt ẩn danh, DevTools, Fullscreen nếu Lock Desktop hoạt động
                if (QASmartClass.Services.ClassControlService.Instance.IsDesktopLocked)
                {
                    // Chặn F11 (122) và F12 (123)
                    if (vkCode == 122 || vkCode == 123)
                    {
                        Log.Information("[KeyboardHook] Blocked browser functional key: VK {Code}", vkCode);
                        return new IntPtr(1);
                    }
                    // Chặn Ctrl + Shift + I (73)
                    if (ctrl && shift && vkCode == 73)
                    {
                        Log.Information("[KeyboardHook] Blocked DevTools shortcut: Ctrl + Shift + I");
                        return new IntPtr(1);
                    }
                    // Chặn Ctrl + N hoặc Ctrl + Shift + N (78)
                    if (ctrl && vkCode == 78)
                    {
                        Log.Information("[KeyboardHook] Blocked new window/incognito shortcut: VK {Code}", vkCode);
                        return new IntPtr(1);
                    }
                }

                // 8. Phím tắt khẩn cấp kỹ thuật viên (Ctrl + Alt + Shift + F12, F12 = 123)
                if (ctrl && alt && shift && vkCode == 123)
                {
                    Log.Information("[KeyboardHook] Emergency bypass hotkey triggered.");
                    AppInstance_RequestEmergencyBypass();
                    return new IntPtr(1);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static void AppInstance_RequestEmergencyBypass()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var win = new Window
                {
                    Title = "Xác nhận Quản trị viên",
                    Width = 350,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Topmost = true,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5")),
                    FontFamily = new FontFamily("Segoe UI")
                };

                var grid = new Grid { Margin = new Thickness(15) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lbl = new TextBlock { Text = "Nhập mật mã quản trị viên để gỡ Hook khẩn cấp:", Margin = new Thickness(0, 0, 0, 10), FontSize = 12 };
                Grid.SetRow(lbl, 0);

                var txt = new PasswordBox { Padding = new Thickness(5), FontSize = 13 };
                Grid.SetRow(txt, 1);

                var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
                
                var btnOk = new Button { Content = "Đồng ý", Width = 75, Padding = new Thickness(5), IsDefault = true, Background = Brushes.DarkRed, Foreground = Brushes.White, Cursor = Cursors.Hand };
                btnOk.Click += (s, e) =>
                {
                    // === UPGRADE_11: Rate limiting — chặn brute force ===
                    if (DateTime.UtcNow < _bypassLockoutUntil)
                    {
                        var remaining = (_bypassLockoutUntil - DateTime.UtcNow).Minutes + 1;
                        MessageBox.Show($"⛔ Đã nhập sai quá {MAX_BYPASS_ATTEMPTS} lần.\nVui lòng thử lại sau {remaining} phút.", "Tạm khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string pw = txt.Password;
                    bool isValid = false;

                    // === UPGRADE_11: Ưu tiên OTP từ Server (nếu có) ===
                    if (!string.IsNullOrEmpty(EmergencyOTP) && pw == EmergencyOTP)
                    {
                        isValid = true;
                        Log.Information("[KeyboardHook] UPGRADE_11: Emergency bypass via Server OTP thành công.");
                    }
                    // Fallback: SHA-256 password (cho trường hợp offline / không có OTP)
                    else if (ComputeSha256(pw) == "460d847dc25d9d76adbc3d110e84247519fa9d9b44c1a71321d8b6356f11c6c4")
                    {
                        isValid = true;
                        Log.Information("[KeyboardHook] UPGRADE_11: Emergency bypass via SHA-256 fallback.");
                    }

                    if (isValid)
                    {
                        _failedBypassAttempts = 0;
                        KeyboardHookHelper.DisableHook();
                        EmergencyOTP = string.Empty; // Invalidate OTP sau khi dùng
                        MessageBox.Show("✅ Đã gỡ bỏ hook bàn phím khẩn cấp thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        win.Close();
                    }
                    else
                    {
                        _failedBypassAttempts++;
                        if (_failedBypassAttempts >= MAX_BYPASS_ATTEMPTS)
                        {
                            _bypassLockoutUntil = DateTime.UtcNow.AddMinutes(LOCKOUT_MINUTES);
                            Log.Warning("[KeyboardHook] UPGRADE_11: Quá {Max} lần nhập sai. Tạm khóa {Min} phút.", MAX_BYPASS_ATTEMPTS, LOCKOUT_MINUTES);
                            MessageBox.Show($"⛔ Đã nhập sai {MAX_BYPASS_ATTEMPTS} lần.\nTạm khóa {LOCKOUT_MINUTES} phút.", "Tạm khóa", MessageBoxButton.OK, MessageBoxImage.Error);
                            win.Close();
                        }
                        else
                        {
                            MessageBox.Show($"❌ Mật mã không chính xác! (Còn {MAX_BYPASS_ATTEMPTS - _failedBypassAttempts} lần thử)", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            txt.Password = string.Empty;
                            txt.Focus();
                        }
                    }
                };

                var btnCancel = new Button { Content = "Hủy", Width = 75, Margin = new Thickness(10, 0, 0, 0), IsCancel = true, Cursor = Cursors.Hand };
                btnCancel.Click += (s, e) => win.Close();

                sp.Children.Add(btnOk);
                sp.Children.Add(btnCancel);

                grid.Children.Add(lbl);
                grid.Children.Add(txt);
                
                var gridParent = new Grid();
                gridParent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                gridParent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                Grid.SetRow(grid, 0);
                Grid.SetRow(sp, 1);
                
                gridParent.Children.Add(grid);
                gridParent.Children.Add(sp);
                gridParent.Margin = new Thickness(15);
                
                win.Content = gridParent;
                txt.Focus();
                win.ShowDialog();
            }));
        }

        private static string ComputeSha256(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
