using OfficeOpenXml;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.IO;
using System.Linq;

namespace QASmartClass.YouthUnion.Services
{
    public static class YouthExportHelper
    {
        public static string GetCurrentSchoolYear()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var yr = db.ClassRosters.Select(c => c.SchoolYear).FirstOrDefault(y => !string.IsNullOrEmpty(y));
                    if (!string.IsNullOrEmpty(yr)) return yr;
                }
            }
            catch { }
            int year = DateTime.Today.Year;
            if (DateTime.Today.Month >= 8)
                return $"{year}-{year + 1}";
            else
                return $"{year - 1}-{year}";
        }

        public static string GetExportPassword()
        {
            var config = AppConfig.Load();
            string schoolCode = config.SchoolCode?.Trim() ?? "";
            if (string.IsNullOrEmpty(schoolCode)) schoolCode = "QA";
            return schoolCode + GetCurrentSchoolYear();
        }

        public static void EncryptWorkbook(ExcelPackage package)
        {
            package.Encryption.Password = GetExportPassword();
        }
    }
}
