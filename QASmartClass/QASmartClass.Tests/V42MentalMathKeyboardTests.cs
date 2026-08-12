using System;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Views.Thinking;
using Xunit;

namespace QASmartClass.Tests
{
    public class V42MentalMathKeyboardTests
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
        public void TestMentalMathTool_KeyboardPlacement_IsRight()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Reset các trường tĩnh để tránh lỗi Thread Affinity
                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                // Add missing static resources to prevent XamlParseException
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                Assert.NotNull(txtAnswer);

                // Kích hoạt focus để ShowPopup chạy
                txtAnswer.Focus();

                var popup = (Popup)popupField.GetValue(null);
                Assert.NotNull(popup);

                // Xác nhận Placement mặc định cho MentalMathTool là Right
                Assert.Equal(PlacementMode.Right, popup.Placement);
            });
        }

        [Fact]
        public void TestTouchNumPad_RemembersDraggedPosition()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Reset các trường tĩnh để tránh lỗi Thread Affinity
                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                var tb1 = new TextBox();
                TouchNumPad.Attach(tb1, step: 1, placement: PlacementMode.Bottom);

                // Kích hoạt hiển thị
                tb1.Focus();

                var popup = (Popup)popupField.GetValue(null);
                Assert.NotNull(popup);

                // Giả lập kéo thả bàn phím ảo bằng cách gán offset và thiết lập biến lưu trữ
                popup.HorizontalOffset = 150;
                popup.VerticalOffset = 250;

                var savedHorzField = typeof(TouchNumPad).GetField("_savedHorizontalOffset", BindingFlags.NonPublic | BindingFlags.Static);
                var savedVertField = typeof(TouchNumPad).GetField("_savedVerticalOffset", BindingFlags.NonPublic | BindingFlags.Static);
                var hasSavedField = typeof(TouchNumPad).GetField("_hasSavedOffset", BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(savedHorzField);
                Assert.NotNull(savedVertField);
                Assert.NotNull(hasSavedField);

                savedHorzField.SetValue(null, 150.0);
                savedVertField.SetValue(null, 250.0);
                hasSavedField.SetValue(null, true);

                // Gọi lại ShowPopup (giả lập focus lại)
                var showMethod = typeof(TouchNumPad).GetMethod("ShowPopup", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(showMethod);

                showMethod.Invoke(null, new object[] { tb1 });

                // Khẳng định offset không bị reset về 0 mà giữ nguyên giá trị đã lưu
                Assert.Equal(150, popup.HorizontalOffset);
                Assert.Equal(250, popup.VerticalOffset);
            });
        }

        [Fact]
        public void TestTouchNumPad_MathKeysVisibility()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                var mathPanelField = typeof(TouchNumPad).GetField("_mathPanel", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(mathPanelField);
                mathPanelField.SetValue(null, null);

                // Attach with enableMathKeys = true
                var tb1 = new TextBox();
                TouchNumPad.Attach(tb1, step: 1, enableMathKeys: true);
                tb1.Focus();

                var mathPanel = (System.Windows.FrameworkElement)mathPanelField.GetValue(null);
                Assert.NotNull(mathPanel);
                Assert.Equal(System.Windows.Visibility.Visible, mathPanel.Visibility);

                // Attach with enableMathKeys = false (default)
                var tb2 = new TextBox();
                TouchNumPad.Attach(tb2, step: 1, enableMathKeys: false);
                tb2.Focus();

                mathPanel = (System.Windows.FrameworkElement)mathPanelField.GetValue(null);
                Assert.NotNull(mathPanel);
                Assert.Equal(System.Windows.Visibility.Collapsed, mathPanel.Visibility);
            });
        }
    }
}
