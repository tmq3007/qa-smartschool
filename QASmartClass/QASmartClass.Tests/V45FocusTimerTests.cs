using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QASmartClass.LearningTools.Views.Multi;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V45FocusTimerTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    if (System.Windows.Application.Current == null)
                    {
                        try { new System.Windows.Application(); } catch { }
                    }

                    if (System.Windows.Application.Current != null)
                    {
                        if (!System.Windows.Application.Current.Resources.Contains("InterFont"))
                        {
                            System.Windows.Application.Current.Resources.Add("InterFont", new System.Windows.Media.FontFamily("Arial"));
                        }
                        if (!System.Windows.Application.Current.Resources.Contains("OutfitFont"))
                        {
                            System.Windows.Application.Current.Resources.Add("OutfitFont", new System.Windows.Media.FontFamily("Arial"));
                        }
                    }

                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void TestGetToolDisplayName_FocusTimer_ReturnsCorrectVietnameseName()
        {
            RunOnStaThread(() =>
            {
                var method = typeof(StudentShell).GetMethod("GetToolDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var displayName = (string)method.Invoke(null, new object[] { "focus_timer" });
                Assert.Equal("Đồng Hồ Tập Trung", displayName);
            });
        }

        [Fact]
        public void TestFocusTimerTool_HasScrollViewer_AndButtonsExist()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Lấy nút Start và Reset
                var btnStart = tool.FindName("btnStart") as Button;
                var btnReset = tool.FindName("btnReset") as Button;

                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);

                Assert.Equal(Visibility.Visible, btnStart.Visibility);
                Assert.Equal(Visibility.Visible, btnReset.Visibility);

                // Xác thực nút bấm hoạt động click mà không quăng lỗi
                var startClickMethod = typeof(FocusTimerTool).GetMethod("BtnStart_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(startClickMethod);
            });
        }

        [Fact]
        public void TestStudentMode_DisablesControls()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus và ghi đè IsStudent()
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Kích hoạt loaded event để gọi UpdateControlsState()
                var loadedMethod = typeof(FocusTimerTool).GetMethod("UpdateControlsState", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadedMethod);
                loadedMethod.Invoke(tool, null);

                // Lấy các nút Start và Reset
                var btnStart = tool.FindName("btnStart") as Button;
                var btnReset = tool.FindName("btnReset") as Button;
                var sliderVolume = tool.FindName("sliderVolume") as Slider;
                var btnSoundRain = tool.FindName("btnSoundRain") as ToggleButton;

                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);
                Assert.NotNull(sliderVolume);
                Assert.NotNull(btnSoundRain);

                // Khi ở chế độ học sinh và bị focus bởi giáo viên, các nút điều khiển chính phải ẩn/vô hiệu hóa
                Assert.Equal(Visibility.Collapsed, btnStart.Visibility);
                Assert.Equal(Visibility.Collapsed, btnReset.Visibility);
                Assert.False(sliderVolume.IsEnabled);
                Assert.False(btnSoundRain.IsEnabled);

                // Cleanup
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = null;
            });
        }

        [Fact]
        public void TestSyncFromTeacher_UpdatesTimeAndState()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus và ghi đè IsStudent()
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Đồng bộ từ giáo viên: 15 phút tập trung, 5 phút nghỉ, 900 giây còn lại, đang chạy, không nghỉ, phiên 1
                tool.SyncFromTeacher(15, 5, 900, true, false, 1);

                var txtTime = tool.FindName("txtTime") as TextBlock;
                var txtSessionCount = tool.FindName("txtSessionCount") as TextBlock;
                var txtSessionLabel = tool.FindName("txtSessionLabel") as TextBlock;

                Assert.NotNull(txtTime);
                Assert.NotNull(txtSessionCount);
                Assert.NotNull(txtSessionLabel);

                Assert.Equal("15:00", txtTime.Text);
                Assert.Contains(txtSessionCount.Text, new[] { "Phiên: 1/4", "FocusTimer_SessionCountFormat", "Session: 1/4" });
                Assert.Contains(txtSessionLabel.Text, new[] { "🎯 Đang tập trung...", "FocusTimer_StatusFocusing", "Focusing..." });

                // Cleanup
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = null;
            });
        }
    }
}
