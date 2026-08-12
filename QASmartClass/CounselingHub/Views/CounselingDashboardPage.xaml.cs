using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.CounselingHub.Views
{
    public partial class CounselingDashboardPage : Page
    {
        private AppDbContext? _db;
        private CounselingService? _service;

        public CounselingDashboardPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
            _service = null;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _service = new CounselingService(_db);
            await LoadDashboardStats();
        }

        private async System.Threading.Tasks.Task LoadDashboardStats()
        {
            if (_service == null) return;
            try
            {
                var stats = await _service.GetDashboardStatsAsync();
                
                TxtTotal.Text = stats.TotalRequests.ToString();
                TxtPending.Text = stats.PendingRequests.ToString();
                TxtScheduled.Text = stats.ScheduledRequests.ToString();
                TxtInProgress.Text = stats.InProgressRequests.ToString();
                TxtCompleted.Text = stats.CompletedRequests.ToString();

                int totalCount = stats.TotalRequests;

                var distList = stats.IssueDistribution.Select(kvp => {
                    double pct = totalCount > 0 ? (double)kvp.Value * 100 / totalCount : 0;
                    return new
                    {
                        ProblemType = kvp.Key,
                        Count = kvp.Value,
                        Percentage = pct,
                        DisplayText = $"{kvp.Value} ca ({pct:0.0}%)",
                        BarColor = GetBarColor(kvp.Key)
                    };
                }).OrderByDescending(x => x.Count).ToList();

                IcDistribution.ItemsSource = distList;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("LoadDashboardStats error: {Err}", ex.Message);
            }
        }

        private string GetBarColor(string problemType)
        {
            switch (problemType)
            {
                case "Học tập": return "#0EA5E9"; // Sky blue
                case "Gia đình": return "#F59E0B"; // Amber
                case "Tâm lý cá nhân":
                case "Tâm lý": return "#8B5CF6"; // Purple
                case "Quan hệ bạn bè":
                case "Giao tiếp": return "#6366F1"; // Indigo
                case "Sức khỏe": return "#10B981"; // Emerald
                default: return "#64748B"; // Slate
            }
        }
    }
}
