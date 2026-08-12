using System;
using System.Reflection;
using System.Threading;
using QASmartClass.LearningTools.Views.Workplace;
using Xunit;

namespace QASmartClass.Tests
{
    public class V40FiveSToolTests
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
        public void TestFiveSTool_Initialization_NoException()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Attempt to instantiate the control, which triggers layout and SizeChanged events.
                // It should compile and instantiate without throwing KeyNotFoundException.
                var tool = new FiveSTool();
                Assert.NotNull(tool);

                // Manually trigger UpdateChart using reflection to ensure it executes without errors.
                var method = typeof(FiveSTool).GetMethod("UpdateChart", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Invoking UpdateChart should not throw KeyNotFoundException
                method.Invoke(tool, null);
            });
        }
    }
}
