using Xunit;
using System;
using System.IO;
using System.Linq;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Views.Multi;

namespace QASmartClass.Tests
{
    public class V97HistoricalDynastiesToolTests
    {
        [Fact]
        public void Test_Database_Initialization_And_Seeding()
        {
            // Verify that dynasties can be fetched and are populated (since we seed them on database initialization)
            var dynasties = DbManager.GetHistoricalDynasties();
            Assert.NotNull(dynasties);
            
            // Check that the count is exactly 32 (20 Vietnam + 12 World)
            Assert.Equal(32, dynasties.Count);
        }

        [Fact]
        public void Test_HistoricalDynasties_ContentIntegrity()
        {
            var dynasties = DbManager.GetHistoricalDynasties();
            
            // Check for Vietnam gap fill: "Bắc thuộc lần 4 & Hậu Trần"
            var gapFill = dynasties.FirstOrDefault(d => d.NameVi == "Bắc thuộc lần 4 & Hậu Trần");
            Assert.NotNull(gapFill);
            Assert.Equal("Vietnam", gapFill.Category);
            Assert.Equal("1407 – 1427", gapFill.Period);

            // Check for added World Dynasties
            var qinHan = dynasties.FirstOrDefault(d => d.NameVi == "Nhà Tần & Nhà Hán");
            Assert.NotNull(qinHan);
            Assert.Equal("World", qinHan.Category);
            Assert.Equal("221 TCN – 220 CN", qinHan.Period);

            var meiji = dynasties.FirstOrDefault(d => d.NameVi == "Nhật Bản thời Minh Trị");
            Assert.NotNull(meiji);
            Assert.Equal("World", meiji.Category);
            Assert.Equal("1868 – 1912", meiji.Period);
        }

        [Fact]
        public void Test_ImageResources_ExistOnDisk()
        {
            // Find base directory
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? imagesDir = null;
            for (int i = 0; i < 5; i++)
            {
                string testPath = Path.Combine(currentDir, "Assets", "Images");
                if (Directory.Exists(testPath))
                {
                    imagesDir = testPath;
                    break;
                }
                string testPath2 = Path.Combine(currentDir, "QASmartClass", "Assets", "Images");
                if (Directory.Exists(testPath2))
                {
                    imagesDir = testPath2;
                    break;
                }
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }

            Assert.NotNull(imagesDir);

            // Assert that the three optimized JPG assets exist
            for (int i = 1; i <= 3; i++)
            {
                string imgPath = Path.Combine(imagesDir, $"app_dynasties_{i}.jpg");
                Assert.True(File.Exists(imgPath), $"Image asset {imgPath} does not exist!");
            }
        }
    }
}
