using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using Serilog;

namespace QASmartClass.Classroom.Helpers
{
    /// <summary>
    /// Chuẩn hóa pattern button guard lặp 55 lần:
    ///   btn.IsEnabled = false; try { await ... } finally { btn.IsEnabled = true; }
    /// </summary>
    internal static class ButtonGuard
    {
        /// <summary>
        /// Vô hiệu hóa nút, chạy action bất đồng bộ, tự bật lại.
        /// Nuốt exception + log để không crash UI.
        /// </summary>
        public static async Task RunAsync(
            Button btn,
            Func<Task> action,
            string errorContext = "")
        {
            btn.IsEnabled = false;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log.Warning("ButtonGuard [{Context}] error: {Err}", errorContext, ex.Message);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Overload cho action không trả Task (sync).
        /// </summary>
        public static async Task RunAsync(
            Button btn,
            Action action,
            string errorContext = "")
        {
            await RunAsync(btn, () =>
            {
                action();
                return Task.CompletedTask;
            }, errorContext);
        }
    }
}
