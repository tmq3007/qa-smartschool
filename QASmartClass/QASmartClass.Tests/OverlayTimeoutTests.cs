using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class OverlayTimeoutTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception? ex = null;
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
        public void TestOverlayTimeoutVariables_ResetOnClose()
        {
            RunOnStaThread(() =>
            {
                // Ensure Application exists for resource merging
                if (Application.Current == null)
                {
                    try
                    {
                        new Application();
                    }
                    catch { }
                }

                // Instantiate StudentShell
                var shell = new StudentShell();

                // Get private field _overlayCreatedTime using Reflection
                var field = typeof(StudentShell).GetField("_overlayCreatedTime", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(field);

                // Set value to simulate active overlay
                field.SetValue(shell, DateTime.UtcNow);

                // Act - Close overlay to trigger reset
                shell.CloseScreenBroadcast();

                // Assert - Check if it reset back to DateTime.MinValue
                var valAfter = (DateTime)field.GetValue(shell);
                Assert.Equal(DateTime.MinValue, valAfter);
            });
        }
    }
}
