using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.LearningTools.Views.Multi;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V44BrainstormFocusTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
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
        public void TestGetToolDisplayName_Brainstorm_ReturnsCorrectVietnameseName()
        {
            RunOnStaThread(() =>
            {
                var method = typeof(StudentShell).GetMethod("GetToolDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var displayName = (string)method.Invoke(null, new object[] { "brainstorm" });
                Assert.Equal("Bức Tường Ý Tưởng", displayName);
            });
        }

        [Fact]
        public void TestStudentShell_ShowToolFocusOverlay_CardHasStretchAlignments()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var shell = new StudentShell();

                var showOverlayMethod = typeof(StudentShell).GetMethod("ShowToolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(showOverlayMethod);

                // Gọi ShowToolFocusOverlay("brainstorm")
                showOverlayMethod.Invoke(shell, new object[] { "brainstorm" });

                var overlayField = typeof(StudentShell).GetField("_toolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);

                var overlay = (Grid)overlayField.GetValue(shell);
                Assert.NotNull(overlay);

                // Tìm card (Border) bên trong overlay
                Border card = null;
                foreach (var child in overlay.Children)
                {
                    if (child is Border b && b.Tag == null) // The main card
                    {
                        card = b;
                        break;
                    }
                }

                Assert.NotNull(card);
                Assert.Equal(HorizontalAlignment.Stretch, card.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Stretch, card.VerticalAlignment);
                Assert.Equal(10.0, card.Margin.Left);
                Assert.Equal(10.0, card.Margin.Top);
            });
        }

        [Fact]
        public void TestBrainstormTool_AddFarNote_CanvasExpandsDynamically()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var tool = new BrainstormTool();

                var addNoteMethod = typeof(BrainstormTool).GetMethod("AddNoteAt", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(addNoteMethod);

                // Thêm ghi chú tại toạ độ xa (1500, 1000)
                addNoteMethod.Invoke(tool, new object[] { "Test Note", Colors.Yellow, 1500.0, 1000.0, false, null });

                // Lấy BoardCanvas để xác thực kích thước rộng/cao của nó tự co giãn theo ghi chú
                var canvasField = typeof(BrainstormTool).GetField("BoardCanvas", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(canvasField);

                var canvas = (Canvas)canvasField.GetValue(tool);
                Assert.NotNull(canvas);

                // Chiều rộng canvas phải bao phủ toạ độ x=1500 + độ rộng note(200) + khoảng đệm (100) = 1800
                Assert.True(canvas.Width >= 1800);
                // Chiều cao canvas phải bao phủ toạ độ y=1000 + độ cao note(180) + khoảng đệm (100) = 1280
                Assert.True(canvas.Height >= 1280);
            });
        }
    }
}
