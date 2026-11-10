using System.Threading.Tasks;

namespace QASmartTouch.Services
{
    public interface IUserInterfaceService
    {
        Task ShowInfoAsync(string message, string title);
        Task<bool> ShowConfirmAsync(string message, string title, bool isWarning = false);
        void PlaySound(string soundType); // "Success", "Error", "Warning"
    }
}
