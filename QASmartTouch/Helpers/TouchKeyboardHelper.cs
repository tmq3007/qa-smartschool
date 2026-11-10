using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_KEYBOARD_HELPER: Điều khiển hiển thị bàn phím ảo (Touch Keyboard / TabTip & OSK)
    /// chuẩn hóa cho cả Máy tính (Desktop Mouse/Keyboard) và Màn hình Tương tác (SMART TOUCH - Touch/Stylus).
    /// 
    /// Nguyên tắc vận hành:
    /// 1. Tự động nhận diện trạng thái hiển thị qua Win32 API để tránh đảo ngược trạng thái (idempotent).
    /// 2. Ưu tiên COM ITipInvocation chuẩn xác cho Windows 10/11 Touch Keyboard ngay cả khi tiến trình TabTip.exe đang chạy nền.
    /// 3. Khi COM ITipInvocation kích hoạt thành công, lập tức kết thúc để tránh xung đột mở đè thêm bàn phím OSK.
    /// 4. Fallback sang OSK.exe (On-Screen Keyboard) khi môi trường không hỗ trợ Modern Touch Keyboard (Desktop PC không có Touch Shell).
    /// 5. Đảm bảo hoạt động đồng bộ trên cả Chuột (Desktop) và Cảm ứng chạm (Touch/Stylus).
    /// </summary>
    public static class TouchKeyboardHelper
    {
        #region Win32 Constants & Native Methods

        private const string WindowClassLegacy = "IPTip_Main_Window";
        private const string WindowClassOsk = "OSKMainClass";
        private const string WindowParentClass1709 = "ApplicationFrameWindow";
        private const string WindowClass1709 = "Windows.UI.Core.CoreWindow";
        private const string WindowCaption1709 = "Microsoft Text Input Application";

        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        #endregion

        #region COM ITipInvocation for Windows 10/11 Touch Keyboard

        [ComImport, Guid("4ce576fa-83dc-4F88-951c-9d0782b4e376")]
        private class UIHostNoLaunch { }

        [ComImport, Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        #endregion

        #region State & Debounce

        private static readonly object _lock = new object();
        private static DateTime _lastShowTime = DateTime.MinValue;
        private const int DebounceMs = 250;

        #endregion

        #region Window Visibility Checks

        /// <summary>
        /// Kiểm tra xem cửa sổ Modern Touch Keyboard (Windows 10/11) có đang hiển thị hay không.
        /// </summary>
        public static bool IsModernTouchKeyboardVisible()
        {
            try
            {
                IntPtr parent = IntPtr.Zero;
                for (int i = 0; i < 10; i++)
                {
                    parent = FindWindowEx(IntPtr.Zero, parent, WindowParentClass1709, null);
                    if (parent == IntPtr.Zero) break;
                    IntPtr wnd = FindWindowEx(parent, IntPtr.Zero, WindowClass1709, WindowCaption1709);
                    if (wnd != IntPtr.Zero && IsWindowVisible(wnd)) return true;
                }

                IntPtr win11 = FindWindow(WindowClass1709, WindowCaption1709);
                if (win11 != IntPtr.Zero && IsWindowVisible(win11)) return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ IsModernTouchKeyboardVisible error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra xem cửa sổ On-Screen Keyboard (OSK) có đang hiển thị hay không.
        /// </summary>
        public static bool IsOskVisible()
        {
            try
            {
                IntPtr oskWnd = FindWindow(WindowClassOsk, null);
                if (oskWnd != IntPtr.Zero && IsWindowVisible(oskWnd)) return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Kiểm tra xem cửa sổ TabTip Legacy có đang hiển thị hay không.
        /// </summary>
        public static bool IsLegacyTabTipVisible()
        {
            try
            {
                IntPtr tipWnd = FindWindow(WindowClassLegacy, null);
                if (tipWnd != IntPtr.Zero && IsWindowVisible(tipWnd)) return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Kiểm tra bất kỳ bàn phím ảo nào (Touch Keyboard hoặc OSK) hiện có đang hiển thị trên màn hình hay không.
        /// </summary>
        public static bool IsKeyboardVisible()
        {
            return IsOskVisible() || IsModernTouchKeyboardVisible() || IsLegacyTabTipVisible();
        }

        #endregion

        /// <summary>
        /// Kích hoạt và hiển thị bàn phím ảo duy nhất.
        /// Chuẩn hóa kép: Ưu tiên Modern Touch Keyboard (cho Màn hình tương tác),
        /// tự động fallback sang OSK (cho Máy tính Desktop).
        /// </summary>
        public static void ShowTouchKeyboard()
        {
            try
            {
                lock (_lock)
                {
                    if ((DateTime.Now - _lastShowTime).TotalMilliseconds < DebounceMs)
                    {
                        return;
                    }
                    _lastShowTime = DateTime.Now;
                }

                // Nếu bất kỳ bàn phím nào đang hiển thị sẵn thì không can thiệp, tránh toggle đóng nhầm
                if (IsKeyboardVisible())
                {
                    return;
                }

                // 1. Ưu tiên kích hoạt Modern Windows Touch Keyboard qua COM ITipInvocation (Chuẩn cho SMART TOUCH)
                try
                {
                    var uiHost = new UIHostNoLaunch();
                    var tipInvocation = (ITipInvocation)uiHost;
                    tipInvocation.Toggle(IntPtr.Zero);
                    Marshal.ReleaseComObject(uiHost);

                    // ✅ Đã gửi lệnh kích hoạt Touch Keyboard thành công.
                    // Kết thúc ngay lập tức để không mở đè thêm bàn phím OSK thứ hai.
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ COM ITipInvocation fallback: {ex.Message}");
                }

                // 2. Thử kích hoạt TabTip.exe trực tiếp nếu COM không khả dụng
                string tabTipPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                    @"microsoft shared\ink\TabTip.exe");

                if (File.Exists(tabTipPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = tabTipPath,
                            UseShellExecute = true
                        });
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Process.Start TabTip error: {ex.Message}");
                    }
                }

                // 3. Fallback sang osk.exe (On-Screen Keyboard) khi môi trường hoàn toàn không hỗ trợ Touch Keyboard
                LaunchOsk();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ ShowTouchKeyboard error: {ex.Message}");
            }
        }

        private static void LaunchOsk()
        {
            IntPtr ptr = IntPtr.Zero;
            bool is64 = Environment.Is64BitProcess;
            try
            {
                if (!is64 && Environment.Is64BitOperatingSystem)
                {
                    Wow64DisableWow64FsRedirection(ref ptr);
                }

                string oskPath = Path.Combine(Environment.SystemDirectory, "osk.exe");
                if (File.Exists(oskPath))
                {
                    var existingOsk = Process.GetProcessesByName("osk");
                    if (existingOsk.Length == 0)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = oskPath,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ LaunchOsk error: {ex.Message}");
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Wow64RevertWow64FsRedirection(ptr);
                }
            }
        }

        /// <summary>
        /// Thu gọn hoặc đóng bàn phím ảo một cách êm ái khi đóng cửa sổ soạn thảo.
        /// </summary>
        public static void HideTouchKeyboard()
        {
            try
            {
                // Đóng TabTip Legacy Window nếu có
                IntPtr tipWnd = FindWindow(WindowClassLegacy, null);
                if (tipWnd != IntPtr.Zero)
                {
                    PostMessage(tipWnd, WM_SYSCOMMAND, SC_CLOSE, 0);
                }

                // Đóng OSK Window nếu có
                IntPtr oskWnd = FindWindow(WindowClassOsk, null);
                if (oskWnd != IntPtr.Zero)
                {
                    PostMessage(oskWnd, WM_SYSCOMMAND, SC_CLOSE, 0);
                }

                // Đóng Modern Touch Keyboard CHỈ KHI nó đang thực sự hiển thị
                if (IsModernTouchKeyboardVisible())
                {
                    try
                    {
                        var uiHost = new UIHostNoLaunch();
                        var tipInvocation = (ITipInvocation)uiHost;
                        tipInvocation.Toggle(IntPtr.Zero);
                        Marshal.ReleaseComObject(uiHost);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ HideTouchKeyboard error: {ex.Message}");
            }
        }
    }
}
