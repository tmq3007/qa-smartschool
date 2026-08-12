using System;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Services
{
    public class WpfUserInterfaceService : IUserInterfaceService
    {
        public Task ShowInfoAsync(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmAsync(string message, string title, bool isWarning = false)
        {
            var img = isWarning ? MessageBoxImage.Warning : MessageBoxImage.Question;
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, img);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public void PlaySound(string soundType)
        {
            try
            {
                switch (soundType)
                {
                    case "Success":
                        Console.Beep(1200, 150);
                        break;
                    case "Error":
                        Console.Beep(400, 400);
                        break;
                    case "Warning":
                        Console.Beep(600, 200);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(600, 200);
                        break;
                }
            }
            catch
            {
                try
                {
                    System.Media.SystemSounds.Beep.Play();
                }
                catch { }
            }
        }
    }
}
