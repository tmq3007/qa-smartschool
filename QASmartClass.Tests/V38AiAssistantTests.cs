using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Services;
using QASmartClass.TeacherHub.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V38AiAssistantTests
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
        public void TestGotFocus_ClearsPlaceholder()
        {
            RunOnStaThread(() =>
            {
                // Ensure WPF Application is initialized
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V38AiAssistantTests] Error: {ex.Message}"); }
                }

                var page = new AiAssistantPage();
                
                // Get TxtInput textbox via reflection
                var txtInput = page.GetType().GetField("TxtInput", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(page) as TextBox;
                Assert.NotNull(txtInput);

                // Initial state - should equal placeholder text
                Assert.Equal("Nhập câu hỏi hoặc yêu cầu hỗ trợ...", txtInput.Text);

                // Simulate GotFocus
                var method = page.GetType().GetMethod("TxtInput_GotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(page, new object[] { txtInput, null });

                // Text should be cleared when focused
                Assert.Equal("", txtInput.Text);
                
                // Foreground color should be changed to #1E293B (Dark slate)
                var brush = txtInput.Foreground as SolidColorBrush;
                Assert.NotNull(brush);
                Assert.Equal(Color.FromRgb(30, 41, 59), brush.Color);
            });
        }

        [Fact]
        public void TestSendMessage_ValidQuery()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V38AiAssistantTests] Error: {ex.Message}"); }
                }

                var page = new AiAssistantPage();
                var txtInput = page.GetType().GetField("TxtInput", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(page) as TextBox;
                Assert.NotNull(txtInput);

                // Set query text
                txtInput.Text = "giáo án";

                // Get static history field to check the size before
                var historyField = typeof(AiAssistantPage).GetField("_conversationHistory", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(historyField);
                var history = historyField.GetValue(null) as List<ChatMessage>;
                Assert.NotNull(history);
                int countBefore = history.Count;

                // Simulate SendMessage
                var sendMessageMethod = page.GetType().GetMethod("SendMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(sendMessageMethod);
                sendMessageMethod.Invoke(page, null);

                // TextBox should be reset to empty (focus is still inside)
                Assert.Equal("", txtInput.Text);

                // Check message count increases by 2 (User message and AI response)
                Assert.Equal(countBefore + 2, history.Count);

                // Assert last messages
                var userMsg = history[history.Count - 2];
                var aiMsg = history[history.Count - 1];

                Assert.Equal("User", userMsg.Sender);
                Assert.Equal("giáo án", userMsg.Text);

                Assert.Equal("AI", aiMsg.Sender);
                Assert.StartsWith("AI-Generated Draft", aiMsg.Text);
            });
        }

        [Fact]
        public void TestSendMessage_EmptyOrPlaceholder_DoesNotSend()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V38AiAssistantTests] Error: {ex.Message}"); }
                }

                var page = new AiAssistantPage();
                var txtInput = page.GetType().GetField("TxtInput", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(page) as TextBox;
                Assert.NotNull(txtInput);

                var historyField = typeof(AiAssistantPage).GetField("_conversationHistory", BindingFlags.NonPublic | BindingFlags.Static);
                var history = historyField.GetValue(null) as List<ChatMessage>;
                Assert.NotNull(history);
                int countBefore = history.Count;

                var sendMessageMethod = page.GetType().GetMethod("SendMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(sendMessageMethod);

                // Scenario A: Text is exactly placeholder
                txtInput.Text = "Nhập câu hỏi hoặc yêu cầu hỗ trợ...";
                sendMessageMethod.Invoke(page, null);
                Assert.Equal(countBefore, history.Count);

                // Scenario B: Text is whitespace
                txtInput.Text = "   ";
                sendMessageMethod.Invoke(page, null);
                Assert.Equal(countBefore, history.Count);
            });
        }
    }
}
