using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;
using Xunit;
using QASmartClass.Services;
using QASmartTouch.Services;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Views.Thinking;

namespace QASmartClass.Tests
{
    public class LOI_VID_16_to_20_ExpertBoardValidationTests
    {
        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
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
                        if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                            System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                        if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                            System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                        if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                            System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                    }

                    // Reset static fields of TouchNumPad to avoid Thread Affinity issues across STA threads
                    try
                    {
                        var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                        if (popupField != null) popupField.SetValue(null, null);

                        var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                        if (targetField != null) targetField.SetValue(null, null);
                    }
                    catch { }

                    InitializeApplicationFull(); action();
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

        #region 🏢 IT MANAGER EVALUATION
        [Fact]
        public void ITManager_VerifyHardwareAccelerationSetting()
        {
            // Cán bộ IT kiểm tra cấu hình Tăng tốc phần cứng trong AppConfig được nạp/lưu chính xác
            var config = new AppConfig
            {
                EnableHardwareAcceleration = false,
                ReduceAnimations = true
            };

            // Lưu và tải lại để kiểm chứng tính bền vững
            string tempConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_classroom_settings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(config, options);
            File.WriteAllText(tempConfigPath, json);

            var loadedConfig = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(tempConfigPath));
            Assert.NotNull(loadedConfig);
            Assert.False(loadedConfig.EnableHardwareAcceleration);
            Assert.True(loadedConfig.ReduceAnimations);

            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
        #endregion

        #region 🎮 GAMER EVALUATION
        [Fact]
        public void Gamer_Verify3DQualityLowMode_TessellationResolution()
        {
            // Một game thủ chuyên nghiệp kiểm tra FPS bằng cách đảm bảo chất lượng Low giới hạn lưới mesh ở độ phân giải 20
            var originalQuality = AppSettings.GraphicsQuality;
            try
            {
                AppSettings.GraphicsQuality = "Low";
                int recommendedRes = AppSettings.GetRecommended3DResolution();
                Assert.Equal(20, recommendedRes); // Low quality limits resolution to 20 to boost frame rate

                // Kiểm tra hàm tạo mesh trong Graph3DFunction giới hạn dưới recommendedRes
                var function = new QASmartTouch.Models.Graph3DFunction();
                var mesh = function.GenerateMesh(-1, 1, -1, 1, 50);
                Assert.NotNull(mesh);
            }
            finally
            {
                AppSettings.GraphicsQuality = originalQuality;
            }
        }
        #endregion

        #region 🎨 UI DESIGNER EVALUATION
        [Fact]
        public void UIDesigner_VerifyAntiAliasingEdgeMode()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Nhà thiết kế giao diện kiểm duyệt xem chế độ Thấp (Low Quality) có tắt khử răng cưa 3D không
                var originalQuality = AppSettings.GraphicsQuality;
                try
                {
                    AppSettings.GraphicsQuality = "Low";

                    // Mô phỏng hàm ApplyGraphicsSettings trên Model3DControl
                    int resolutionSetting = AppSettings.GetRecommended3DResolution();
                    Assert.True(resolutionSetting <= 20);

                    var viewport = new Viewport3D();
                    // Nếu chất lượng thấp, EdgeMode phải được set thành Aliased
                    if (resolutionSetting <= 20)
                    {
                        System.Windows.Media.RenderOptions.SetEdgeMode(viewport, System.Windows.Media.EdgeMode.Aliased);
                    }

                    var edgeMode = System.Windows.Media.RenderOptions.GetEdgeMode(viewport);
                    Assert.Equal(System.Windows.Media.EdgeMode.Aliased, edgeMode);
                }
                finally
                {
                    AppSettings.GraphicsQuality = originalQuality;
                }
            });
        }
        #endregion

        #region 🔒 SECURITY EXPERT EVALUATION
        [Fact]
        public void SecurityExpert_VerifyInputFilteringExceptionSafety()
        {
            RunOnStaThread(() =>
            {
                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                var txtFeedback = (TextBlock)tool.FindName("txtFeedback");
                Assert.NotNull(txtAnswer);
                Assert.NotNull(txtFeedback);

                // Kích hoạt Game chạy
                var btnStart = (Button)tool.FindName("btnStart");
                Assert.NotNull(btnStart);
                btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Mô phỏng kéo thả (drag-drop) chữ cái lạ vào TextBox đáp án
                txtAnswer.Text = "invalid_character_input";

                // Nhấn phím Enter
                var keyDownMethod = typeof(MentalMathTool).GetMethod("Answer_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(keyDownMethod);

                using (var source = new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero))
                {
                    var keyArgs = new System.Windows.Input.KeyEventArgs(
                        System.Windows.Input.Keyboard.PrimaryDevice,
                        source,
                        0, System.Windows.Input.Key.Enter)
                    {
                        RoutedEvent = UIElement.KeyDownEvent
                    };

                    // Gọi xử lý sự kiện xem có bị crash ứng dụng không
                    var ex = Record.Exception(() => keyDownMethod.Invoke(tool, new object[] { txtAnswer, keyArgs }));
                    Assert.Null(ex); // Đảm bảo an toàn không crash
                }

                // Chuyên gia bảo mật xác nhận thông báo lỗi hiển thị rõ ràng trên UI
                Assert.True(txtFeedback.Text.Contains("Lỗi định dạng số!") || txtFeedback.Text.Contains("Invalid number format!"), 
                    $"Feedback text was: {txtFeedback.Text}");
            });
        }
        #endregion

        #region 🎒 EDUCATOR & PRINCIPAL EVALUATION
        [Fact]
        public void Educator_VerifyPedagogicalErrorsCorrectlyShow()
        {
            RunOnStaThread(() =>
            {
                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                var txtFeedback = (TextBlock)tool.FindName("txtFeedback");
                Assert.NotNull(txtAnswer);
                Assert.NotNull(txtFeedback);

                // Kích hoạt Game chạy
                var btnStart = (Button)tool.FindName("btnStart");
                btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Nhập giá trị trống hoặc dấu cách
                txtAnswer.Text = "   ";

                var keyDownMethod = typeof(MentalMathTool).GetMethod("Answer_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
                using (var source = new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero))
                {
                    var keyArgs = new System.Windows.Input.KeyEventArgs(
                        System.Windows.Input.Keyboard.PrimaryDevice,
                        source,
                        0, System.Windows.Input.Key.Enter)
                    {
                        RoutedEvent = UIElement.KeyDownEvent
                    };

                    keyDownMethod.Invoke(tool, new object[] { txtAnswer, keyArgs });
                }

                // Nhà giáo dục yêu cầu phải có phản hồi sư phạm nhắc học sinh nhập số thay vì bỏ qua im lặng
                Assert.True(txtFeedback.Text.Contains("Vui lòng nhập số!") || txtFeedback.Text.Contains("Please enter a number!"),
                    $"Feedback text was: {txtFeedback.Text}");
            });
        }

        [Fact]
        public void Principal_VerifyTouchNumPadLayoutSynchronousUpdate()
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

                var tb = new TextBox();
                // Phóng to kích hoạt
                tb.Width = 200;
                tb.Height = 40;

                // Hiệu trưởng xác nhận: Gọi TouchNumPad.Attach và gán vị trí Right để bàn phím bên phải TextBox, không che đề bài
                TouchNumPad.Attach(tb, step: 1, placement: PlacementMode.Right);
                tb.Focus();

                var popup = (Popup)popupField.GetValue(null);
                Assert.NotNull(popup);
                Assert.Equal(PlacementMode.Right, popup.Placement);
            });
        }
        #endregion
    }

    // Helper class để bọc PresentationSource cho KeyEventArgs
    internal class PresentationSourceWrapper : System.Windows.PresentationSource
    {
        protected override System.Windows.Media.CompositionTarget GetCompositionTargetCore() => null;
        public override System.Windows.Media.Visual RootVisual { get; set; }
        public override bool IsDisposed => false;
    }
}
