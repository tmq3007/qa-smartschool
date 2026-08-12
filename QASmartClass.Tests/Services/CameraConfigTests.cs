using System;
using System.IO;
using Xunit;
using QASmartTouch.Services.Camera;
using QASmartTouch.Models.Camera;

namespace QASmartClass.Tests.Services
{
    public class CameraConfigTests : IDisposable
    {
        private readonly CameraConfigService _configService;

        public CameraConfigTests()
        {
            _configService = new CameraConfigService();
        }

        public void Dispose()
        {
            _configService.DeleteConfig();
        }

        [Fact]
        public void CameraConfig_Load_Default_Profile_If_No_Config_Exists()
        {
            _configService.DeleteConfig();
            var profile = _configService.Load();
            Assert.NotNull(profile);
            Assert.Equal(1280, profile.Width);
            Assert.Equal(720, profile.Height);
            Assert.Equal(30, profile.FPS);
        }

        [Fact]
        public void CameraConfig_SaveAndLoad_WorksCorrectly()
        {
            var profile = new CameraProfile
            {
                SelectedCameraId = "TestCameraDeviceId123",
                Width = 1920,
                Height = 1080,
                FPS = 15,
                AutoStartOnLaunch = true
            };

            _configService.Save(profile);

            Assert.True(_configService.ConfigExists());

            var reloaded = _configService.Load();
            Assert.Equal("TestCameraDeviceId123", reloaded.SelectedCameraId);
            Assert.Equal(1920, reloaded.Width);
            Assert.Equal(1080, reloaded.Height);
            Assert.Equal(15, reloaded.FPS);
            Assert.True(reloaded.AutoStartOnLaunch);
        }
    }
}
