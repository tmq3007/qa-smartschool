using System;
using Xunit;
using QASmartClass.Services;
using QASmartClass.YouthUnion;

namespace QASmartClass.Tests
{
    public class YouthUnionTests
    {
        [Fact]
        public void YouthMapper_Mode0_ASCII_Mapping_Works()
        {
            // Set Mode 0 (ASCII compatibility)
            var config = AppConfig.Load();
            config.YouthUnionDbEncodingMode = 0;
            config.Save();

            // Test member type maps
            Assert.Equal("Đoàn viên", YouthMapper.MapMemberTypeToUI(YouthConstants.YouthMemberTypes.DoanVien));
            Assert.Equal("Đội viên", YouthMapper.MapMemberTypeToUI(YouthConstants.YouthMemberTypes.DoiVien));
            
            Assert.Equal(YouthConstants.YouthMemberTypes.DoanVien, YouthMapper.MapMemberTypeToDb("Đoàn viên"));
            Assert.Equal(YouthConstants.YouthMemberTypes.DoiVien, YouthMapper.MapMemberTypeToDb("Đội viên"));

            // Test position maps
            Assert.Equal("Thành viên", YouthMapper.MapPositionToUI(YouthConstants.YouthPositions.ThanhVien));
            Assert.Equal("Bí thư", YouthMapper.MapPositionToUI(YouthConstants.YouthPositions.BiThu));
            Assert.Equal("Phó Bí thư", YouthMapper.MapPositionToUI(YouthConstants.YouthPositions.PhoBT));
            Assert.Equal("Ủy viên BCH", YouthMapper.MapPositionToUI(YouthConstants.YouthPositions.UVBCH));

            Assert.Equal(YouthConstants.YouthPositions.ThanhVien, YouthMapper.MapPositionToDb("Thành viên"));
            Assert.Equal(YouthConstants.YouthPositions.BiThu, YouthMapper.MapPositionToDb("Bí thư"));
            Assert.Equal(YouthConstants.YouthPositions.PhoBT, YouthMapper.MapPositionToDb("Phó Bí thư"));
            Assert.Equal(YouthConstants.YouthPositions.UVBCH, YouthMapper.MapPositionToDb("Ủy viên BCH"));
        }

        [Fact]
        public void YouthMapper_Mode1_Unicode_Mapping_Works()
        {
            // Set Mode 1 (Unicode native)
            var config = AppConfig.Load();
            config.YouthUnionDbEncodingMode = 1;
            config.Save();

            // Test member type maps (should return the same string directly)
            Assert.Equal("Đoàn viên", YouthMapper.MapMemberTypeToUI("Đoàn viên"));
            Assert.Equal("Đoàn viên", YouthMapper.MapMemberTypeToDb("Đoàn viên"));

            // Test position maps
            Assert.Equal("Bí thư", YouthMapper.MapPositionToUI("Bí thư"));
            Assert.Equal("Bí thư", YouthMapper.MapPositionToDb("Bí thư"));
            
            // Clean up config to default Mode 0
            config.YouthUnionDbEncodingMode = 0;
            config.Save();
        }

        [Fact]
        public void YouthMapper_StatusMapping_IsModeIndependent()
        {
            // Status should always map to Vietnamese UI
            Assert.Equal("Đang hoạt động", YouthMapper.MapStatusToUI(YouthConstants.YouthStatus.Active));
            Assert.Equal("Tạm dừng", YouthMapper.MapStatusToUI(YouthConstants.YouthStatus.Inactive));
            Assert.Equal("Đã chuyển đi", YouthMapper.MapStatusToUI(YouthConstants.YouthStatus.Transferred));

            Assert.Equal(YouthConstants.YouthStatus.Active, YouthMapper.MapStatusToDb("Đang hoạt động"));
            Assert.Equal(YouthConstants.YouthStatus.Inactive, YouthMapper.MapStatusToDb("Tạm dừng"));
            Assert.Equal(YouthConstants.YouthStatus.Transferred, YouthMapper.MapStatusToDb("Đã chuyển đi"));
        }
    }
}
