using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Services
{
    public class NetworkDiagnosticsService
    {
        public static bool IsRunningAsAdmin()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Firewall] Lỗi kiểm tra quyền Administrator: {Err}", ex.Message);
                return false;
            }
        }

        public static async Task<bool> CheckFirewallRulesExistAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetFirewallRule -Name 'QASmartClass_Web_In' -ErrorAction Stop\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        proc?.WaitForExit();
                        return proc?.ExitCode == 0;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Firewall] Lỗi kiểm tra quy tắc tường lửa: {Err}", ex.Message);
                    return false;
                }
            });
        }

        public static async Task<bool> RunFirewallAutoFixAsync()
        {
            if (!IsRunningAsAdmin())
            {
                Log.Information("[Firewall] Tiến trình hiện tại chưa có quyền Admin. Đang yêu cầu nâng quyền qua UAC...");
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return false;

                var psiAdmin = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--configure-firewall",
                    Verb = "runas", // Triggers UAC dialog
                    UseShellExecute = true
                };

                try
                {
                    bool success = await Task.Run(() =>
                    {
                        using (var p = Process.Start(psiAdmin))
                        {
                            p?.WaitForExit();
                            return p?.ExitCode == 0;
                        }
                    });
                    return success;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Firewall] Người dùng từ chối cấp quyền Administrator hoặc lỗi UAC: {Err}", ex.Message);
                    return false;
                }
            }

            // Nếu đã chạy với quyền Admin, chạy trực tiếp PowerShell để cấu hình các port
            return await ExecutePowerShellRulesRegistrationAsync();
        }

        private static async Task<bool> ExecutePowerShellRulesRegistrationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // PowerShell script đăng ký luật tường lửa và bắt lỗi Group Policy (GPO)
                    string script = @"
                        $ErrorActionPreference = 'Stop'
                        try {
                            # Inbound Rules
                            New-NetFirewallRule -Name 'QASmartClass_Web_In' -DisplayName 'QASmartClass_Web_In' -Description 'QA SmartClass Web Services' -Direction Inbound -Protocol TCP -LocalPort 8080-8084 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_Control_In' -DisplayName 'QASmartClass_Control_In' -Description 'QA SmartClass Control Panel' -Direction Inbound -Protocol TCP -LocalPort 9090 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_State_In' -DisplayName 'QASmartClass_State_In' -Description 'QA SmartClass State Service' -Direction Inbound -Protocol TCP -LocalPort 5050,6060 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_Broadcast_In' -DisplayName 'QASmartClass_Broadcast_In' -Description 'QA SmartClass Broadcast Service' -Direction Inbound -Protocol UDP -LocalPort 7070-7071 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue

                            # === UPGRADE_05: Thêm rule cho UDP Multicast Screen Broadcast port 8088 (đồng bộ với UdpScreenBroadcastService.MulticastPort) ===
                            New-NetFirewallRule -Name 'QASmartClass_Multicast_In' -DisplayName 'QASmartClass_Multicast_In' -Description 'QA SmartClass UDP Multicast Screen Broadcast (port 8088)' -Direction Inbound -Protocol UDP -LocalPort 8088 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue

                             # Outbound Rules
                            New-NetFirewallRule -Name 'QASmartClass_Web_Out' -DisplayName 'QASmartClass_Web_Out' -Description 'QA SmartClass Web Outbound' -Direction Outbound -Protocol TCP -LocalPort 8080-8084 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_Control_Out' -DisplayName 'QASmartClass_Control_Out' -Description 'QA SmartClass Control Outbound' -Direction Outbound -Protocol TCP -LocalPort 9090 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_State_Out' -DisplayName 'QASmartClass_State_Out' -Description 'QA SmartClass State Outbound' -Direction Outbound -Protocol TCP -LocalPort 5050,6060 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            New-NetFirewallRule -Name 'QASmartClass_Broadcast_Out' -DisplayName 'QASmartClass_Broadcast_Out' -Description 'QA SmartClass Broadcast Outbound' -Direction Outbound -Protocol UDP -LocalPort 7070-7071 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue

                            # === UPGRADE_05: Thêm rule cho UDP Multicast Screen Broadcast port 8088 (đồng bộ với UdpScreenBroadcastService.MulticastPort) ===
                            New-NetFirewallRule -Name 'QASmartClass_Multicast_Out' -DisplayName 'QASmartClass_Multicast_Out' -Description 'QA SmartClass UDP Multicast Screen Broadcast Outbound (port 8088)' -Direction Outbound -Protocol UDP -LocalPort 8088 -Action Allow -Profile Private,Public -Enabled True -ErrorAction SilentlyContinue
                            
                            Write-Output 'SUCCESS'
                        }
                        catch {
                            Write-Error $_.Exception.Message
                            exit 2
                        }
                    ";
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace(Environment.NewLine, " ")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    using (var proc = Process.Start(psi))
                    {
                        if (proc == null) return false;
                        string output = proc.StandardOutput.ReadToEnd();
                        string error = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();

                        if (proc.ExitCode != 0)
                        {
                            Log.Warning("[Firewall] Không thể thiết lập rules. Có thể bị chặn bởi GPO hoặc phần mềm diệt virus: {Err}", error);
                            return false;
                        }
                        
                        Log.Information("[Firewall] Đã tự động cấu hình luật tường lửa thành công.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Firewall] Gặp lỗi nghiêm trọng khi thiết lập Rules: {Err}", ex.Message);
                    return false;
                }
            });
        }
    }
}
