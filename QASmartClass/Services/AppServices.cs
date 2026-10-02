using QASmartClass.Data;
using System.Windows;

namespace QASmartClass.Services
{
    /// <summary>
    /// Static helper truy cập các service dùng chung trong ứng dụng.
    /// Giảm tight coupling với Application.Current cast.
    /// 
    /// Usage: AppServices.Database thay vì ((QASmartTouch.App)Application.Current).Database
    /// </summary>
    public static class AppServices
    {
        public static IUserInterfaceService UIService { get; set; } = new WpfUserInterfaceService();


        /// <summary>
        /// Database context chung cho toàn ứng dụng (startup/singleton).
        /// Được khởi tạo trong App.xaml.cs khi startup.
        /// * Không thread-safe — dùng CreateDb() cho multi-thread scenarios.
        /// </summary>
        public static AppDbContext? Database { get; set; }

        /// <summary>
        /// Tạo DbContext mới — thread-safe, dùng cho Services/Pages cần isolation.
        /// Caller chịu trách nhiệm Dispose.
        /// </summary>
        public static AppDbContext CreateDb() => new AppDbContext();

        /// <summary>
        /// Khởi tạo tất cả services — gọi 1 lần trong App.OnStartup()
        /// </summary>
        public static void Initialize(AppDbContext db)
        {
            Database = db;
        }
    }
}
