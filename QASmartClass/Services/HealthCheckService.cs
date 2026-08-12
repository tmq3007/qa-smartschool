﻿using QASmartClass.Data;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service kiểm tra tình trạng hệ thống — chạy khi startup + mỗi 30 phút
    /// </summary>
    public class HealthCheckService
    {
        private Timer? _timer;
        private static HealthCheckService? _instance;

        public static HealthCheckService Instance => _instance ??= new HealthCheckService();

        private HealthCheckService() { }

        /// <summary>Khởi động HealthCheck: chạy ngay + lặp mỗi 30 phút</summary>
        public void Start()
        {
            RunCheck();
            _timer = new Timer(_ => RunCheck(), null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
            Log.Information("[HealthCheck] Service started — interval=30min");
        }

        /// <summary>Dừng HealthCheck timer</summary>
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>Thực hiện kiểm tra sức khỏe hệ thống</summary>
        public void RunCheck()
        {
            var dbStatus = CheckDbConnection();
            var diskStatus = CheckDiskSpace();
            var licenseStatus = CheckLicenseStatus();

            Log.Information("[HealthCheck] DB={DB}, Disk={Disk}, License={License}",
                dbStatus, diskStatus, licenseStatus);
        }

        /// <summary>Kiểm tra kết nối DB</summary>
        private string CheckDbConnection()
        {
            try
            {
                using var db = new AppDbContext();
                var count = db.Students.Count();
                return $"OK ({count} students)";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>Kiểm tra dung lượng ổ đĩa</summary>
        private string CheckDiskSpace()
        {
            try
            {
                var appDir = AppPaths.RootDir;
                var drive = new DriveInfo(Path.GetPathRoot(appDir) ?? "C:");
                var freeGB = Math.Round((double)drive.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                var status = freeGB < 1 ? "LOW" : "OK";
                return $"{status} ({freeGB}GB free)";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>Kiểm tra trạng thái license</summary>
        private string CheckLicenseStatus()
        {
            try
            {
                var license = QASmartTouch.Services.License.LicenseService.Instance.CheckLicense();
                return license.ToString();
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }
    }
}

