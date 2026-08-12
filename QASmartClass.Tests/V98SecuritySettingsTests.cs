using Xunit;
using System;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using QASmartClass.Classroom.ViewModels;
using QASmartClass.Utilities;

namespace QASmartClass.Tests
{
    public class V98SecuritySettingsTests
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;

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
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("STA thread error: " + ex.Message, ex);
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        private void StartAutoDismissMessageBox(string title, int delayMs = 100)
        {
            var thread = new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(delayMs);
                for (int i = 0; i < 50; i++)
                {
                    IntPtr hwnd = FindWindow("#32770", title);
                    if (hwnd != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        [Fact]
        public void Test_DpapiEncryptionDecryption_Success()
        {
            string originalKey = "AIzaSyTestKey_DPAPI_123456";
            
            // Encrypt
            string encrypted = CryptoHelper.EncryptWithDpapi(originalKey);
            Assert.False(string.IsNullOrEmpty(encrypted));
            Assert.NotEqual(originalKey, encrypted);

            // Decrypt
            string decrypted = CryptoHelper.DecryptWithDpapi(encrypted);
            Assert.Equal(originalKey, decrypted);
        }

        [Fact]
        public void Test_DpapiDecryption_FallbackForLegacyPlaintext()
        {
            string legacyPlaintext = "AIzaSyPlaintextKey123";
            
            // Decrypting a plaintext key that was NOT encrypted with DPAPI (or is not valid Base64 DPAPI format)
            // should fail and return empty string, allowing the VM fallback logic to use the original string.
            string decrypted = CryptoHelper.DecryptWithDpapi(legacyPlaintext);
            Assert.True(string.IsNullOrEmpty(decrypted));
        }

        [Fact]
        public void Test_SettingsViewModel_ApiKeyMaskingAndToggling()
        {
            RunOnStaThread(() =>
            {
                var vm = new SettingsViewModel();
                
                // Test defaults
                Assert.False(vm.IsApiKeyVisible);
                Assert.True(vm.IsApiKeyReadOnly);
                Assert.Equal("👁️", vm.ApiKeyVisibilityIcon);

                // Set value while hidden (simulating initialization/load)
                // Note: Normally loaded via load logic into school config or Properties.Settings.
                // We will simulate toggling visibility first, then setting the value, then hiding.
                vm.IsApiKeyVisible = true;
                vm.ApiKey = "AIzaSyNewTestKey";
                Assert.Equal("AIzaSyNewTestKey", vm.ApiKey);

                // Hide
                vm.IsApiKeyVisible = false;
                Assert.True(vm.IsApiKeyReadOnly);
                Assert.Equal("👁️", vm.ApiKeyVisibilityIcon);
                // ApiKey should be masked with 24 bullets
                Assert.Equal(new string('•', 24), vm.ApiKey);

                // Show again
                vm.IsApiKeyVisible = true;
                Assert.False(vm.IsApiKeyReadOnly);
                Assert.Equal("🔒", vm.ApiKeyVisibilityIcon);
                Assert.Equal("AIzaSyNewTestKey", vm.ApiKey);

                // Toggle command
                vm.ToggleApiKeyVisibilityCommand.Execute(null);
                Assert.False(vm.IsApiKeyVisible);
                Assert.Equal(new string('•', 24), vm.ApiKey);
            });
        }

        [Fact]
        public void Test_SettingsViewModel_SaveAndLoadIntegration()
        {
            RunOnStaThread(() =>
            {
                var vm = new SettingsViewModel();
                
                // Set test API Key
                vm.IsApiKeyVisible = true;
                vm.ApiKey = "AIzaSyIntegrationKey_987";
                
                // Save - start the auto-dismiss thread for MessageBox
                StartAutoDismissMessageBox("Lưu API Key");
                vm.SaveApiKeyCommand.Execute(null);

                // We test integration by verifying the loaded value on a new VM instance.

                // Load setting back into a new VM instance to test Load settings logic
                var vmNew = new SettingsViewModel();
                
                var loadMethod = typeof(SettingsViewModel).GetMethod("LoadApiKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadMethod);
                loadMethod.Invoke(vmNew, null);

                // Verify loaded value is hidden but matches target key when shown
                Assert.False(vmNew.IsApiKeyVisible);
                Assert.Equal(new string('•', 24), vmNew.ApiKey);

                vmNew.IsApiKeyVisible = true;
                Assert.Equal("AIzaSyIntegrationKey_987", vmNew.ApiKey);
            });
        }
    }
}
