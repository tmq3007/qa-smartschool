using System;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;
        
        public static string CurrentToken { get; private set; } = string.Empty;
        public static string CurrentRole { get; private set; } = string.Empty;
        public static string CurrentUserSsoId { get; private set; } = string.Empty;
        public static string CurrentUserName { get; private set; } = "Võ Minh Em";

        public AuthService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<bool> LoginAsync(string ssoUserId, string fullName, string role)
        {
            try
            {
                // HỖ TRỢ TEST UI: Giả lập mạng chậm 1s và đăng nhập thành công luôn
                // (Vì hiện tại chúng ta chưa chạy Server Backend API)
                await Task.Delay(1000);
                
                CurrentToken = "MOCK_TOKEN_DEV_ONLY";
                CurrentRole = role;
                CurrentUserSsoId = ssoUserId;
                CurrentUserName = fullName;
                return true;

                /* --- KHI NÀO CÓ BACKEND THÌ MỞ ĐOẠN NÀY RA ---
                var request = new { ssoUserId, fullName, role };
                var response = await _apiService.PostAsync<object, LoginResponse>("/Auth/generate-test-token", request);
                
                if (response != null && !string.IsNullOrEmpty(response.Token))
                {
                    CurrentToken = response.Token;
                    CurrentRole = role;
                    CurrentUserSsoId = ssoUserId;
                    return true;
                }
                return false;
                --------------------------------------------- */
            }
            catch (Exception)
            {
                // Xử lý lỗi kết nối
                return false;
            }
        }

        public void Logout()
        {
            CurrentToken = null;
            CurrentRole = null;
            CurrentUserSsoId = null;
            CurrentUserName = null;
        }

        private class LoginResponse
        {
            public string Token { get; set; }
        }
    }
}
