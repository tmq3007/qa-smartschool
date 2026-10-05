using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;

namespace QASmartClass.Helpers
{
    /// <summary>
    /// FileAssociationHelper — Quản lý đăng ký liên kết tệp (.qasc) với Windows
    /// Hỗ trợ cả môi trường chạy trực tiếp (Portable/Dev) và bộ cài đặt (Installer).
    /// </summary>
    public static class FileAssociationHelper
    {
        private const string PROG_ID = "QASmartClass.Lecture";
        private const string EXTENSION = ".qasc";
        private const string FILE_DESCRIPTION = "Bài giảng QA SmartClass";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        /// <summary>
        /// Đảm bảo định dạng tệp .qasc được liên kết với ứng dụng trong HKCU (không yêu cầu quyền Administrator).
        /// Cho phép người dùng click đúp tệp bài giảng trong Explorer để tự động mở ứng dụng và nạp bài giảng.
        /// </summary>
        public static void EnsureFileAssociation()
        {
            try
            {
                string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QASmartClass.exe");
                }

                if (!File.Exists(exePath)) return;

                bool changed = false;

                // 1. Đăng ký phần mở rộng .qasc trỏ về ProgID QASmartClass.Lecture
                using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{EXTENSION}"))
                {
                    if (extKey != null)
                    {
                        var currentProgId = extKey.GetValue("") as string;
                        if (currentProgId != PROG_ID)
                        {
                            extKey.SetValue("", PROG_ID);
                            changed = true;
                        }
                    }
                }

                // 2. Đăng ký ProgID và tên mô tả
                using (var progKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{PROG_ID}"))
                {
                    if (progKey != null)
                    {
                        var currentDesc = progKey.GetValue("") as string;
                        if (currentDesc != FILE_DESCRIPTION)
                        {
                            progKey.SetValue("", FILE_DESCRIPTION);
                            changed = true;
                        }
                    }
                }

                // 3. Đăng ký biểu tượng Icon cho tệp bài giảng
                using (var iconKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{PROG_ID}\DefaultIcon"))
                {
                    if (iconKey != null)
                    {
                        string iconTarget = $"\"{exePath}\",0";
                        var currentIcon = iconKey.GetValue("") as string;
                        if (currentIcon != iconTarget)
                        {
                            iconKey.SetValue("", iconTarget);
                            changed = true;
                        }
                    }
                }

                // 4. Đăng ký lệnh mở shell\open\command
                using (var cmdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{PROG_ID}\shell\open\command"))
                {
                    if (cmdKey != null)
                    {
                        string targetCommand = $"\"{exePath}\" \"%1\"";
                        var currentCmd = cmdKey.GetValue("") as string;
                        if (currentCmd != targetCommand)
                        {
                            cmdKey.SetValue("", targetCommand);
                            changed = true;
                        }
                    }
                }

                // 5. Nếu có thay đổi, thông báo hệ thống Windows cập nhật bộ nhớ đệm Shell
                if (changed)
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                    Log.Information("[FileAssociation] Đã đăng ký thành công liên kết định dạng tệp .qasc cho CurrentUser.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[FileAssociation] Không thể cập nhật liên kết tệp .qasc trong Windows Registry");
            }
        }
    }
}
