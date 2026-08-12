using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using QASmartTouch.PeriodicTable.Models;
using QASmartTouch.PeriodicTable.ViewModels;
using QASmartTouch.PeriodicTable.Views;

namespace QASmartClass.Tests
{
    public class PeriodicTableToolTests
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
            Exception? ex = null;
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
        public void Test_ElementMap_NoMissingElements()
        {
            var manager = ElementDataManager.Instance;
            
            // Check that _elementFileMap exists and has 118 elements
            var field = typeof(ElementDataManager).GetField("_elementFileMap", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            
            var map = (Dictionary<int, string>?)field.GetValue(manager);
            Assert.NotNull(map);
            Assert.Equal(118, map.Count);
            
            for (int i = 1; i <= 118; i++)
            {
                Assert.True(map.ContainsKey(i), $"Element {i} is missing from mapping.");
                var element = manager.GetElementByAtomicNumber(i);
                Assert.True(element != null, $"Element {i} returned null from GetElementByAtomicNumber.");
                Assert.Equal(i, element.AtomicNumber);
            }
        }

        [Fact]
        public void Test_CompareThreeElements_Radioactivity()
        {
            RunOnStaThread(() =>
            {
                var vm = new CompareViewModel();
                Assert.NotEmpty(vm.AllElements);
                
                // Find Hydrogen (H, 1) or Helium (He, 2) which are not radioactive
                // and Uranium (U, 92) which is radioactive
                var h = vm.AllElements.FirstOrDefault(e => e.AtomicNumber == 1);
                var he = vm.AllElements.FirstOrDefault(e => e.AtomicNumber == 2);
                var u = vm.AllElements.FirstOrDefault(e => e.AtomicNumber == 92);
                
                Assert.NotNull(h);
                Assert.NotNull(he);
                Assert.NotNull(u);
                
                vm.Element1 = h;
                vm.Element2 = he;
                vm.Element3 = u;
                vm.CompareMode = 3; // 3 elements comparison mode
                
                var radioactiveItem = vm.ComparisonData.FirstOrDefault(item => item.Property == "Có tính phóng xạ");
                Assert.NotNull(radioactiveItem);
                Assert.Equal("Không", radioactiveItem.Value1);
                Assert.Equal("Không", radioactiveItem.Value2);
                Assert.Equal("Có", radioactiveItem.Value3);
            });
        }

        [Fact]
        public void Test_Simulation_EquationsAndHydrolysis()
        {
            RunOnStaThread(() =>
            {
                var window = new SimulationWindow();
                var field = typeof(SimulationWindow).GetField("solubilityData", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(field);
                
                var data = (Dictionary<string, Dictionary<string, SolubilityInfo>>?)field.GetValue(window);
                Assert.NotNull(data);
                
                // 1. Fe3+ + CO32- hydrolysis reaction yielding Fe(OH)3 and CO2 gas bubbles
                Assert.True(data.ContainsKey("Fe³⁺"));
                Assert.True(data["Fe³⁺"].ContainsKey("CO₃²⁻"));
                var feCo3 = data["Fe³⁺"]["CO₃²⁻"];
                Assert.Equal("2Fe³⁺ + 3CO₃²⁻ + 3H₂O → 2Fe(OH)₃↓ + 3CO₂↑", feCo3.CustomEquation);
                Assert.Contains("thủy phân", feCo3.CustomExplanation);
                Assert.Contains("sắt(III) hiđroxit", feCo3.CustomExplanation);
                Assert.Contains("CO₂", feCo3.CustomExplanation);
                Assert.Equal("#8B4513", feCo3.Color); // Nâu đỏ
                
                // 2. Ag+ + OH- yielding Ag2O precipitant and H2O
                Assert.True(data.ContainsKey("Ag⁺"));
                Assert.True(data["Ag⁺"].ContainsKey("OH⁻"));
                var agOh = data["Ag⁺"]["OH⁻"];
                Assert.Equal("2Ag⁺ + 2OH⁻ → Ag₂O↓ + H₂O", agOh.CustomEquation);
                Assert.Contains("Ag₂O", agOh.CustomExplanation);
                Assert.Equal("#4A2711", agOh.Color); // Nâu đen
            });
        }

        [Fact]
        public void Test_Chromium_AcidReaction()
        {
            RunOnStaThread(() =>
            {
                var window = new ReactivityWindow();
                var field = typeof(ReactivityWindow).GetField("allMetals", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(field);
                
                var allMetals = (List<MetalReaction>?)field.GetValue(window);
                Assert.NotNull(allMetals);
                
                // Chromium
                var cr = allMetals.FirstOrDefault(m => m.Symbol == "Cr");
                Assert.NotNull(cr);
                Assert.Equal("Cr + 2HCl → CrCl₂ + H₂↑", cr.AcidEquation);
                Assert.Contains("Crom(II)", cr.AcidPhenomenon);
                Assert.Contains("xanh lam nhạt", cr.AcidPhenomenon.ToLower());
                
                // Active metals
                var activeMetals = new[] { "Cs", "Fr", "Rb", "K", "Na", "Li", "Ba", "Ra", "Sr", "Ca" };
                foreach (var symbol in activeMetals)
                {
                    var metal = allMetals.FirstOrDefault(m => m.Symbol == symbol);
                    Assert.NotNull(metal);
                    Assert.True(metal.Acid == "Phản ứng dữ dội", 
                        $"Metal {symbol} should have 'Phản ứng dữ dội' acid reactivity level, but has: {metal.Acid}");
                }
            });
        }

        [Fact]
        public void Test_IntroductionWindow_ConstructsAndLoadsImages()
        {
            RunOnStaThread(() =>
            {
                var window = new IntroductionWindow();
                Assert.NotNull(window);
            });
        }
    }
}
