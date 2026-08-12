using QASmartClass.Data;
using QASmartClass.YouthUnion.Services;
using QASmartClass.YouthUnion;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.YouthUnion.Views
{
    public partial class YouthUnionDashboard : Page
    {
        private readonly YouthUnionService _service;

        public YouthUnionDashboard()
        {
            InitializeComponent();
            _service = new YouthUnionService();
Unloaded += (s, e) => { _service?.Dispose(); };
            Loaded += async (_, __) => await LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                // Stats
                var members = await _service.GetAllMembersAsync();
                var activities = await _service.GetActivitiesAsync();
                var total = members.Count;
                var active = members.Count(m => m.Status == YouthConstants.YouthStatus.Active);
                
                var currentPeriod = $"HK1-{DateTime.Now.Year}"; // Simplified for dashboard
                var feeStats = await _service.GetFeeStatsAsync(currentPeriod);
                
                var feeRate = total > 0 ? (feeStats.Paid * 100 / total) : 0;
                var now = DateTime.Now;
                var monthActs = activities.Count(a => a.Date.Month == now.Month && a.Date.Year == now.Year);
                var completedActs = activities.Count(a => a.Status == YouthConstants.PlanStatus.Completed && a.Date.Month == now.Month && a.Date.Year == now.Year);
                var avgScore = total > 0 ? members.Average(m => m.TotalScore) : 0;
                var topMember = members.OrderByDescending(m => m.TotalScore).FirstOrDefault();

                TxtTotalMembers.Text = total.ToString();
                TxtActiveMembers.Text = $"Đang sinh hoạt: {active}";
                TxtMonthActivities.Text = monthActs.ToString();
                TxtCompletedAct.Text = $"Đã hoàn thành: {completedActs}";
                TxtFeeRate.Text = $"{feeRate}%";
                TxtFeePaid.Text = $"Đã đóng: {feeStats.Paid}/{total}";
                TxtAvgScore.Text = avgScore.ToString("0");
                TxtTopMember.Text = topMember != null ? $"Top: {topMember.StudentName}" : "";

                // Recent Activities
                var recent = activities.OrderByDescending(a => a.Date).Take(5).ToList();
                IcRecentActivities.Items.Clear();
                foreach (var a in recent)
                {
                    var statusColor = a.Status == YouthConstants.PlanStatus.Completed ? "#10B981" : a.Status == YouthConstants.PlanStatus.Approved ? "#3B82F6" : "#94A3B8";
                    var row = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    var rowSp = new StackPanel();
                    var titleRow = new DockPanel();
                    titleRow.Children.Add(new TextBlock { Text = a.Title, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) });

                    var badge = new Border
                    {
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString(statusColor)!,
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    var statusText = a.Status switch
                    {
                        "Draft" => "Bản nháp",
                        "Approved" => "Đã duyệt",
                        "Completed" => "Hoàn thành",
                        _ => a.Status
                    };
                    badge.Child = new TextBlock { Text = statusText, FontSize = 11, Foreground = Brushes.White };
                    titleRow.Children.Add(badge);
                    rowSp.Children.Add(titleRow);
                    rowSp.Children.Add(new TextBlock { Text = $"{a.Date:dd/MM/yyyy}  {a.Participants}", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 3, 0, 0) });
                    row.Child = rowSp;
                    IcRecentActivities.Items.Add(row);
                }

                // Top 5 Members
                var top5 = members.OrderByDescending(m => m.TotalScore).Take(5).ToList();
                IcTopMembers.Items.Clear();
                int rank = 1;
                foreach (var m in top5)
                {
                    var medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" };
                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
                    row.Children.Add(new TextBlock { Text = medal, FontSize = 18, Width = 30, VerticalAlignment = VerticalAlignment.Center });
                    var info = new StackPanel();
                    info.Children.Add(new TextBlock { Text = m.StudentName, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) });
                    var posText = m.Position switch
                    {
                        "Secretary" => "Bí thư",
                        "ViceSecretary" => "Phó Bí thư",
                        "Member" => "Ủy viên BCH",
                        "Regular" => "Đoàn viên",
                        _ => m.Position
                    };
                    info.Children.Add(new TextBlock { Text = $"{m.ClassName}  {posText}", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
                    row.Children.Add(info);
                    var scoreTxt = new TextBlock { Text = $"{m.TotalScore} pts", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
                    row.Children.Add(scoreTxt);
                    IcTopMembers.Items.Add(row);
                    rank++;
                }
            }
            catch (Exception ex) { Log.Warning("Dashboard load error: {Err}", ex.Message); }
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var members = await _service.GetAllMembersAsync();
                var reportService = new YouthReportService();
                
                // For demonstration, save to user's documents
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = $"DanhSachDoanVien_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string fullPath = System.IO.Path.Combine(docPath, fileName);
                
                await reportService.GenerateMemberListPdfAsync(members, fullPath);
                
                MessageBox.Show($"Đã xuất file báo cáo thành công tại:\n{fullPath}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error exporting PDF in YouthUnionDashboard");
            }
        }
    }
}

