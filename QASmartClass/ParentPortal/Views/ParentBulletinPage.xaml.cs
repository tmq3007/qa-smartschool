using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentBulletinPage : Page
    {
        private readonly BulletinService _bulletinService;
        private readonly AppDbContext? _dbLocal;
        private readonly bool _ownsDb;

        public ParentBulletinPage()
        {
            InitializeComponent();
            if (QASmartClass.Services.AppServices.Database != null)
            {
                _bulletinService = new BulletinService(QASmartClass.Services.AppServices.Database);
                _ownsDb = false;
            }
            else
            {
                _dbLocal = new AppDbContext();
                _bulletinService = new BulletinService(_dbLocal);
                _ownsDb = true;
            }
            Loaded += Page_Loaded;
            Unloaded += (s, e) => { if (_ownsDb) _dbLocal?.Dispose(); };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBulletins();
        }

        private void LoadBulletins()
        {
            try
            {
                var bulletins = _bulletinService.GetBulletins("PH");
                var items = bulletins.Select(b => new
                {
                    b.Title,
                    b.Content,
                    DateDisplay = $"Ngày đăng: {b.CreatedAt:dd/MM/yyyy HH:mm}",
                    PriorityIcon = b.Priority == "Urgent" ? "🔴" : (b.Priority == "High" ? "🟠" : "🔵"),
                    PriorityBg = b.Priority == "Urgent" ? "#FEE2E2" : (b.Priority == "High" ? "#FFF7ED" : "#EFF6FF")
                }).ToList();

                LvBulletins.ItemsSource = items;
                TxtCount.Text = $"{items.Count} thông báo";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải thông báo cho phụ huynh");
            }
        }
    }
}

