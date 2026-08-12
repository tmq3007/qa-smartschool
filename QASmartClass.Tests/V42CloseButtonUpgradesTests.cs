using Xunit;
using System;
using System.Threading;
using System.Windows;
using QASmartTouch.Controllers;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class V42CloseButtonUpgradesTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    // Ensure Application instance is clean
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                    }
                    catch { }

                    if (Application.Current == null)
                    {
                        new Application();
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
        public void Test_StudentCheck_DoesNotThrow()
        {
            RunOnStaThread(() =>
            {
                // Verify IsStudent() checks cleanly
                bool isStudent = TeachingActionHelper.IsStudent();
                Assert.False(isStudent); // Default context is teacher unless specified
            });
        }
    }
}
