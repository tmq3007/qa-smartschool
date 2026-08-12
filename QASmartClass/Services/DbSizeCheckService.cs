using System;
using System.IO;
using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class DbSizeCheckService
    {
        private readonly AppDbContext _db;

        public DbSizeCheckService(AppDbContext db)
        {
            _db = db;
        }

        public DbSizeStatus CheckDbSize()
        {
            var status = new DbSizeStatus();
            try
            {
                // Read limit from DB
                var limitSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "DbSizeLimit");
                int limitMb = 500;
                if (limitSetting != null && int.TryParse(limitSetting.Value, out int parsed))
                {
                    limitMb = parsed;
                }

                // Read action from DB
                var actionSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Database_OverLimitAction");
                string action = actionSetting?.Value ?? "WarnOnly";

                string dbPath = AppPaths.DatabaseFile;
                double sizeMb = 0;
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    sizeMb = fileInfo.Length / (1024.0 * 1024.0);
                }

                status.LimitMb = limitMb;
                status.CurrentSizeMb = sizeMb;
                status.Action = action;
                status.IsOverLimit = sizeMb > limitMb;
            }
            catch
            {
                // Fallback defaults on error
                status.LimitMb = 500;
                status.CurrentSizeMb = 0;
                status.Action = "WarnOnly";
                status.IsOverLimit = false;
            }
            return status;
        }
    }

    public class DbSizeStatus
    {
        public bool IsOverLimit { get; set; }
        public double CurrentSizeMb { get; set; }
        public int LimitMb { get; set; }
        public string Action { get; set; } = "WarnOnly";
    }
}
