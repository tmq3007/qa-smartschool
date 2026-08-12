using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using QASmartClass.Data;
using QASmartClass.LearningTools.Views.Multi;

namespace QASmartClass.Tests
{
    public class V53NoiseMonitorAndDbLockTests
    {
        [Fact]
        public void TestNoiseMonitorTool_Unloaded_ShouldDispose()
        {
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    // Create instance in STA thread
                    var tool = new NoiseMonitorTool();
                    
                    // Programmatically raise the Unloaded event to trigger Dispose
                    tool.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.UnloadedEvent));
                    
                    // Verify that the control is not null and completed event cleanly
                    Assert.NotNull(tool);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"NoiseMonitorTool unloaded test failed: {ex.Message}");
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public async Task TestAppDbContext_ConcurrentWrites_ShouldSucceedWithoutDbLocked()
        {
            var tasks = new System.Collections.Generic.List<Task>();
            for (int i = 0; i < 5; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(async () =>
                {
                    using (var db = new AppDbContext())
                    {
                        var log = new EventLog
                        {
                            EventType = "CONCURRENCY_TEST",
                            Actor = $"Thread_{threadId}",
                            Details = $"Concurrent write test from thread {threadId}",
                            Timestamp = DateTime.Now
                        };
                        db.EventLogs.Add(log);
                        
                        // Mix sync and async writes to verify both locks
                        if (threadId % 2 == 0)
                        {
                            db.SaveChanges();
                        }
                        else
                        {
                            await db.SaveChangesAsync();
                        }
                        
                        // Clean up
                        db.EventLogs.Remove(log);
                        if (threadId % 2 == 0)
                        {
                            db.SaveChanges();
                        }
                        else
                        {
                            await db.SaveChangesAsync();
                        }
                    }
                }));
            }
            
            // If any thread fails due to SQLite locking/busy, Task.WhenAll will propagate the exception
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }
    }
}
