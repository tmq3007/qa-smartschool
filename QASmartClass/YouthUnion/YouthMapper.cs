using System;
using System.Collections.Generic;
using System.Linq;
using QASmartClass.Services;

namespace QASmartClass.YouthUnion
{
    public static class YouthMapper
    {
        private static readonly Dictionary<string, string> MemberTypeMap = new()
        {
            { YouthConstants.YouthMemberTypes.DoanVien, "Đoàn viên" },
            { YouthConstants.YouthMemberTypes.DoiVien, "Đội viên" }
        };

        private static readonly Dictionary<string, string> PositionMap = new()
        {
            { YouthConstants.YouthPositions.ThanhVien, "Thành viên" },
            { YouthConstants.YouthPositions.BiThu, "Bí thư" },
            { YouthConstants.YouthPositions.PhoBT, "Phó Bí thư" },
            { YouthConstants.YouthPositions.UVBCH, "Ủy viên BCH" }
        };

        private static readonly Dictionary<string, string> StatusMap = new()
        {
            { YouthConstants.YouthStatus.Active, "Đang hoạt động" },
            { YouthConstants.YouthStatus.Inactive, "Tạm dừng" },
            { YouthConstants.YouthStatus.Transferred, "Đã chuyển đi" }
        };

        public static string MapMemberTypeToUI(string dbValue)
        {
            var config = AppConfig.Load();
            if (config.YouthUnionDbEncodingMode == 1) return dbValue; // Mode 1: Already accented in DB
            
            return MemberTypeMap.TryGetValue(dbValue, out var uiValue) ? uiValue : dbValue;
        }

        public static string MapMemberTypeToDb(string uiValue)
        {
            var config = AppConfig.Load();
            if (config.YouthUnionDbEncodingMode == 1) return uiValue; // Mode 1: Save accented directly
            
            var key = MemberTypeMap.FirstOrDefault(x => x.Value == uiValue).Key;
            return key ?? YouthConstants.YouthMemberTypes.DoanVien;
        }

        public static string MapPositionToUI(string dbValue)
        {
            var config = AppConfig.Load();
            if (config.YouthUnionDbEncodingMode == 1) return dbValue; // Mode 1: Already accented in DB
            
            return PositionMap.TryGetValue(dbValue, out var uiValue) ? uiValue : dbValue;
        }

        public static string MapPositionToDb(string uiValue)
        {
            var config = AppConfig.Load();
            if (config.YouthUnionDbEncodingMode == 1) return uiValue; // Mode 1: Save accented directly
            
            var key = PositionMap.FirstOrDefault(x => x.Value == uiValue).Key;
            return key ?? YouthConstants.YouthPositions.ThanhVien;
        }

        public static string MapStatusToUI(string dbValue)
        {
            // Status stays standard ("Active", "Inactive", "Transferred") in DB, but always shown accented in UI
            return StatusMap.TryGetValue(dbValue, out var uiValue) ? uiValue : dbValue;
        }

        public static string MapStatusToDb(string uiValue)
        {
            var key = StatusMap.FirstOrDefault(x => x.Value == uiValue).Key;
            return key ?? YouthConstants.YouthStatus.Active;
        }
    }
}
