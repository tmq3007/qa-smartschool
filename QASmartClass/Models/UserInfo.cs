namespace QASmartTouch.Models
{
    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public bool RememberCredentials { get; set; } = true;
        public bool IsTrialAccount { get; set; } = false;
        public DateTime? LastLogin { get; set; }
    }
}
