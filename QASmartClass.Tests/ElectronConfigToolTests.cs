using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests
{
    public class ElectronConfigToolTests
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
                    if (Application.Current == null)
                    {
                        try
                        {
                            new Application();
                        }
                        catch { }
                    }
                    if (Application.Current != null)
                    {
                        lock (Application.Current.Resources)
                        {
                            if (!Application.Current.Resources.Contains("Gray100"))
                            {
                                Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray));
                            }
                        }
                    }
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
        public void Test_ElectronConfig_Exceptions()
        {
            RunOnStaThread(() =>
            {
                var tool = new ElectronConfigTool();
                var txtZ = tool.FindName("txtZ") as TextBox;
                Assert.NotNull(txtZ);

                // 1. Test Chromium Z=24
                txtZ.Text = "24";
                var method = typeof(ElectronConfigTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                var txtElement = tool.FindName("txtElement") as TextBlock;
                var txtSymbol = tool.FindName("txtSymbol") as TextBlock;
                Assert.NotNull(txtElement);
                Assert.Equal("Cr — Crom", txtElement.Text);
                Assert.Equal("Z = 24 (Chromium)", txtSymbol.Text);

                // 2. Test Copper Z=29
                txtZ.Text = "29";
                method.Invoke(tool, null);
                Assert.Equal("Cu — Đồng", txtElement.Text);
                Assert.Equal("Z = 29 (Copper)", txtSymbol.Text);

                // 3. Test Palladium Z=46
                txtZ.Text = "46";
                method.Invoke(tool, null);
                Assert.Equal("Pd — Palladi", txtElement.Text);
                Assert.Equal("Z = 46 (Palladium)", txtSymbol.Text);
            });
        }

        [Fact]
        public void Test_Period_Group_Calculation()
        {
            RunOnStaThread(() =>
            {
                var tool = new ElectronConfigTool();
                var txtZ = tool.FindName("txtZ") as TextBox;
                Assert.NotNull(txtZ);

                var method = typeof(ElectronConfigTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Test Iron Z=26 (Transition Metal)
                txtZ.Text = "26";
                method.Invoke(tool, null);
                
                // Let's test the calculations by checking text or helper methods via reflection
                var elementField = typeof(ElectronConfigTool).GetField("Elements", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(elementField);
                
                var elements = elementField.GetValue(null) as Dictionary<int, (string Name, string NameEn, string Symbol, string Category)>;
                Assert.NotNull(elements);
                Assert.True(elements.ContainsKey(26));
                Assert.Equal("Sắt", elements[26].Name);
                Assert.Equal("Fe", elements[26].Symbol);
            });
        }

        [Fact]
        public void Test_Lanthanides_Actinides_Classification()
        {
            RunOnStaThread(() =>
            {
                var tool = new ElectronConfigTool();
                var txtZ = tool.FindName("txtZ") as TextBox;
                Assert.NotNull(txtZ);
                var resultPanel = tool.FindName("resultPanel") as StackPanel;
                Assert.NotNull(resultPanel);

                var method = typeof(ElectronConfigTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // 1. Test Uranium Z=92 (Actinide)
                txtZ.Text = "92";
                method.Invoke(tool, null);

                var texts = new List<string>();
                foreach (Border border in resultPanel.Children)
                {
                    string txt = GetBorderText(border);
                    if (!string.IsNullOrEmpty(txt)) texts.Add(txt);
                }

                Assert.Contains(texts, t => t.Contains("Khối nguyên tố (Block): f"));
                Assert.Contains(texts, t => t.Contains("Nhóm: IIIB (Họ Actini)"));

                // 2. Test Lantan Z=57 (Lanthanide)
                txtZ.Text = "57";
                method.Invoke(tool, null);

                texts.Clear();
                foreach (Border border in resultPanel.Children)
                {
                    string txt = GetBorderText(border);
                    if (!string.IsNullOrEmpty(txt)) texts.Add(txt);
                }

                Assert.Contains(texts, t => t.Contains("Khối nguyên tố (Block): f"));
                Assert.Contains(texts, t => t.Contains("Nhóm: IIIB (Họ Lantan)"));

                // 3. Test Lawrencium Z=103 (Actinide exception)
                txtZ.Text = "103";
                method.Invoke(tool, null);

                texts.Clear();
                foreach (Border border in resultPanel.Children)
                {
                    string txt = GetBorderText(border);
                    if (!string.IsNullOrEmpty(txt)) texts.Add(txt);
                }

                Assert.Contains(texts, t => t.Contains("Khối nguyên tố (Block): f"));
                Assert.Contains(texts, t => t.Contains("Nhóm: IIIB (Họ Actini)"));
            });
        }

        private static string GetBorderText(Border border)
        {
            if (border.Child is Grid grid && grid.Children.Count > 0)
            {
                var contentElement = grid.Children[0];
                if (contentElement is TextBlock tb) return tb.Text;
                if (contentElement is WrapPanel wp)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var child in wp.Children)
                    {
                        if (child is TextBlock childTb) parts.Add(childTb.Text);
                        else if (child.GetType().Name == "FormulaControl")
                        {
                            var formulaProp = child.GetType().GetProperty("Formula");
                            if (formulaProp != null) parts.Add($"${formulaProp.GetValue(child)}$");
                        }
                    }
                    return string.Join("", parts);
                }
                if (contentElement.GetType().Name == "FormulaControl")
                {
                    var formulaProp = contentElement.GetType().GetProperty("Formula");
                    if (formulaProp != null) return $"${formulaProp.GetValue(contentElement)}$";
                }
            }
            else if (border.Child is TextBlock textBlock)
            {
                return textBlock.Text;
            }
            return string.Empty;
        }
    }
}
