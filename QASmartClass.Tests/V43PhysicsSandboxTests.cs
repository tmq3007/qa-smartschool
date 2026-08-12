using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Views.Multi;
using Xunit;

namespace QASmartClass.Tests
{
    public class V43PhysicsSandboxTests
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

        [Fact]
        public void TestPhysicsSandbox_LaserOnPrism_DoesNotExplodeRays()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V43PhysicsSandboxTests] Error: {ex.Message}"); }
                }

                var tool = new PhysicsSandboxTool();

                // Thêm Laser và Lăng kính (Prism) vào danh sách objects thông qua Reflection
                var objectsField = typeof(PhysicsSandboxTool).GetField("_objects", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(objectsField);

                // Lấy định nghĩa class PhysicsObject
                var physicsObjectType = typeof(PhysicsSandboxTool).GetNestedType("PhysicsObject", BindingFlags.NonPublic);
                Assert.NotNull(physicsObjectType);

                var objectsList = (System.Collections.IList)objectsField.GetValue(tool);
                Assert.NotNull(objectsList);

                // Laser đặt tại (100, 100)
                var laserObj = Activator.CreateInstance(physicsObjectType);
                physicsObjectType.GetProperty("Type").SetValue(laserObj, Enum.Parse(physicsObjectType.GetProperty("Type").PropertyType, "Laser"));
                physicsObjectType.GetProperty("X").SetValue(laserObj, 100.0);
                physicsObjectType.GetProperty("Y").SetValue(laserObj, 100.0);

                // Prism đặt tại (200, 80) chắn ngang tia laser
                var prismObj = Activator.CreateInstance(physicsObjectType);
                physicsObjectType.GetProperty("Type").SetValue(prismObj, Enum.Parse(physicsObjectType.GetProperty("Type").PropertyType, "Prism"));
                physicsObjectType.GetProperty("X").SetValue(prismObj, 200.0);
                physicsObjectType.GetProperty("Y").SetValue(prismObj, 80.0);

                objectsList.Add(laserObj);
                objectsList.Add(prismObj);

                // Thực thi vẽ tia sáng bằng Reflection gọi RedrawRays()
                var redrawMethod = typeof(PhysicsSandboxTool).GetMethod("RedrawRays", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(redrawMethod);

                redrawMethod.Invoke(tool, null);

                // Lấy danh sách tia sáng _rayLines để kiểm tra số lượng
                var rayLinesField = typeof(PhysicsSandboxTool).GetField("_rayLines", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(rayLinesField);

                var rayLines = (List<Line>)rayLinesField.GetValue(tool);
                Assert.NotNull(rayLines);

                // Nếu sửa đổi thành công, số lượng tia phải bằng 8 (1 tia gốc và 7 tia khúc xạ dispersion)
                // Nếu chưa sửa đổi, số lượng tia sẽ bùng nổ lên tới 19608 tia và gây treo ứng dụng.
                Assert.Equal(8, rayLines.Count);
            });
        }
    }
}
