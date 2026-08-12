using System.Threading.Tasks;
using QASmartClass.Services;

namespace QASmartClass.Tests
{
    public class MockUserInterfaceService : IUserInterfaceService
    {
        public bool ConfirmResult { get; set; } = true;
        public string LastMessage { get; set; } = string.Empty;
        public string LastSoundPlayed { get; set; } = string.Empty;
        public System.Collections.Generic.List<string> PlayedSounds { get; } = new();

        public Task ShowInfoAsync(string message, string title)
        {
            LastMessage = message;
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmAsync(string message, string title, bool isWarning = false)
        {
            LastMessage = message;
            return Task.FromResult(ConfirmResult);
        }

        public void PlaySound(string soundType)
        {
            LastSoundPlayed = soundType;
            PlayedSounds.Add(soundType);
        }
    }
}
