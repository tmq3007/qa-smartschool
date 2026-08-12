using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Staff.ViewModels
{
    public class CanteenTopItem
    {
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public string TotalRevenueStr => $"{TotalRevenue:N0} đ";
    }

    public class IncidentSummaryItem
    {
        public string Severity { get; set; } = "";
        public string SeverityDisplay { get; set; } = "";
        public int Count { get; set; }
        public string ColorHex { get; set; } = "#64748B";
    }

    public class GateStatsItem
    {
        public string Action { get; set; } = "";
        public int Count { get; set; }
        public string ColorHex { get; set; } = "#64748B";
    }

    public class VisitorPurposeItem
    {
        public string Purpose { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#3B82F6";
    }

    public class VisitorHostItem
    {
        public string TeacherName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ReportsAuditLogDisplay
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IntegrityStatus { get; set; } = string.Empty;
        public string IntegrityColor { get; set; } = "#4B5563";
    }

    public partial class ReportsViewModel : ObservableObject
    {
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private int _totalGateAccess;
        [ObservableProperty] private int _pendingIncidents;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _selectedPeriod = "Hôm nay";

        [ObservableProperty] private ObservableCollection<CanteenTopItem> _topProducts = new();
        [ObservableProperty] private ObservableCollection<IncidentSummaryItem> _incidentSummary = new();
        [ObservableProperty] private ObservableCollection<GateStatsItem> _gateStats = new();
        [ObservableProperty] private System.Windows.Visibility _noRevenueVisibility = System.Windows.Visibility.Collapsed;

        // Dynamic Incident Count (Nhiệm vụ 4)
        [ObservableProperty] private int _totalIncidentsCount = 1;

        // Visitor reports properties (Nhiệm vụ 3 & 5)
        [ObservableProperty] private int _totalVisitors;
        [ObservableProperty] private int _activeVisitorsInSchool;
        [ObservableProperty] private double _averageStayDuration;
        [ObservableProperty] private ObservableCollection<VisitorPurposeItem> _visitPurposes = new();
        [ObservableProperty] private ObservableCollection<VisitorHostItem> _topHosts = new();
        [ObservableProperty] private ObservableCollection<SecurityLog> _currentVisitors = new();

        // Audit log properties (Nhiệm vụ 5)
        [ObservableProperty] private ObservableCollection<ReportsAuditLogDisplay> _auditLogs = new();
        [ObservableProperty] private string _searchAuditText = string.Empty;
        [ObservableProperty] private string _selectedAuditActionType = "Tất cả";
        [ObservableProperty] private ObservableCollection<string> _auditActionTypes = new() { "Tất cả", "Cấu hình hệ thống", "Thiết bị di động", "Hành động công văn", "Báo cáo sự cố", "Khác" };

        public ReportsViewModel()
        {
            _ = LoadStatsAsync();
            _ = LoadAuditLogsAsync();
        }

        partial void OnSearchAuditTextChanged(string value)
        {
            _ = LoadAuditLogsAsync();
        }

        partial void OnSelectedAuditActionTypeChanged(string value)
        {
            _ = LoadAuditLogsAsync();
        }

        partial void OnSelectedPeriodChanged(string value)
        {
            _ = LoadStatsAsync();
        }

        [RelayCommand]
        public async Task LoadStatsAsync()
        {
            IsLoading = true;
            try
            {
                using var db = new AppDbContext();
                DateTime startDate = DateTime.Today;
                DateTime endDate = DateTime.Now.AddDays(1); // include current day fully

                if (SelectedPeriod == "7 ngày qua")
                {
                    startDate = DateTime.Today.AddDays(-7);
                }
                else if (SelectedPeriod == "30 ngày qua")
                {
                    startDate = DateTime.Today.AddDays(-30);
                }

                // 1. Canteen revenue & top selling products
                var walletLogs = await db.EventLogs
                    .Where(l => l.EventType == "WalletTransaction" && l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .ToListAsync();

                decimal revenue = 0;
                var productCounts = new System.Collections.Generic.Dictionary<string, (int Count, decimal Total)>();

                foreach (var log in walletLogs)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Type", out var type) && type.GetString() == "Canteen_Deduction")
                        {
                            decimal amount = 0;
                            if (root.TryGetProperty("Amount", out var amt)) amount = amt.GetDecimal();
                            revenue += amount;

                            string items = "Suất ăn Canteen";
                            if (root.TryGetProperty("Items", out var its)) items = its.GetString() ?? "Suất ăn Canteen";

                            if (!productCounts.ContainsKey(items))
                            {
                                productCounts[items] = (0, 0);
                            }
                            var current = productCounts[items];
                            productCounts[items] = (current.Count + 1, current.Total + amount);
                        }
                    }
                    catch { }
                }

                TotalRevenue = revenue;
                var topList = productCounts
                    .Select(p => new CanteenTopItem { Name = p.Key, Quantity = p.Value.Count, TotalRevenue = p.Value.Total })
                    .OrderByDescending(p => p.Quantity)
                    .Take(5)
                    .ToList();
                TopProducts = new ObservableCollection<CanteenTopItem>(topList);
                NoRevenueVisibility = topList.Any() ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                // 2. Incident Summary
                var incidentLogs = await db.EventLogs
                    .Where(l => l.EventType == "Incident" && l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .ToListAsync();

                int pendingCount = 0;
                var severityCounts = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Low", 0 },
                    { "Medium", 0 },
                    { "High", 0 },
                    { "Critical", 0 }
                };

                foreach (var log in incidentLogs)
                {
                    string severity = "Medium";
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Severity", out var sev)) severity = sev.GetString() ?? "Medium";
                    }
                    catch { }

                    if (severityCounts.ContainsKey(severity))
                    {
                        severityCounts[severity]++;
                    }
                    else
                    {
                        severityCounts[severity] = 1;
                    }
                }

                var incSummaryList = severityCounts.Select(s => new IncidentSummaryItem
                {
                    Severity = s.Key,
                    Count = s.Value,
                    SeverityDisplay = s.Key switch
                    {
                        "Low" => "Thấp",
                        "Medium" => "Trung bình",
                        "High" => "Cao",
                        "Critical" => "Nghiêm trọng",
                        _ => s.Key
                    },
                    ColorHex = s.Key switch
                    {
                        "Low" => "#10B981",
                        "Medium" => "#F59E0B",
                        "High" => "#EF4444",
                        "Critical" => "#7F1D1D",
                        _ => "#64748B"
                    }
                }).ToList();
                IncidentSummary = new ObservableCollection<IncidentSummaryItem>(incSummaryList);
                PendingIncidents = incidentLogs.Count; 

                // Dynamic incident count maximum (Nhiệm vụ 4)
                TotalIncidentsCount = incidentLogs.Count > 0 ? incidentLogs.Count : 1;

                // 3. Gate access statistics
                var gateLogs = await db.EventLogs
                    .Where(l => (l.EventType == "GateCheckIn" || l.EventType == "GateCheckOut") && l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .ToListAsync();

                int inCount = gateLogs.Count(l => l.EventType == "GateCheckIn");
                int outCount = gateLogs.Count(l => l.EventType == "GateCheckOut");

                TotalGateAccess = gateLogs.Count;

                var gateStatsList = new System.Collections.Generic.List<GateStatsItem>
                {
                    new GateStatsItem { Action = "Vào trường", Count = inCount, ColorHex = "#10B981" },
                    new GateStatsItem { Action = "Ra trường", Count = outCount, ColorHex = "#3B82F6" }
                };
                GateStats = new ObservableCollection<GateStatsItem>(gateStatsList);

                // 4. Visitor Reports stats query logic (Nhiệm vụ 3)
                var visitorLogs = await db.SecurityLogs
                    .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .ToListAsync();

                var checkInLogs = visitorLogs.Where(l => l.EventType == "CheckIn").ToList();
                TotalVisitors = checkInLogs.Count;
                ActiveVisitorsInSchool = checkInLogs.Count(l => !l.IsCheckedOut);
                
                CurrentVisitors = new ObservableCollection<SecurityLog>(
                    checkInLogs.Where(l => !l.IsCheckedOut).OrderByDescending(l => l.Timestamp).ToList()
                );

                // Average stay duration calculations
                double totalMinutes = 0;
                int pairedCount = 0;
                var checkOutLogs = visitorLogs.Where(l => l.EventType == "CheckOut").ToList();

                foreach (var inLog in checkInLogs)
                {
                    var outLog = checkOutLogs
                        .Where(o => o.BadgeNumber == inLog.BadgeNumber && o.Timestamp > inLog.Timestamp)
                        .OrderBy(o => o.Timestamp)
                        .FirstOrDefault();
                    
                    if (outLog != null)
                    {
                        var duration = (outLog.Timestamp - inLog.Timestamp).TotalMinutes;
                        if (duration > 0 && duration < 1440)
                        {
                            totalMinutes += duration;
                            pairedCount++;
                        }
                    }
                }
                AverageStayDuration = pairedCount > 0 ? Math.Round(totalMinutes / pairedCount, 1) : 0;

                // Visit purpose distribution
                var purposeCounts = checkInLogs
                    .GroupBy(l => string.IsNullOrEmpty(l.VisitPurpose) ? "Khác" : l.VisitPurpose)
                    .Select(g => new { Purpose = g.Key, Count = g.Count() })
                    .ToList();

                int totalCheckInsForPurpose = checkInLogs.Count > 0 ? checkInLogs.Count : 1;
                var purposeList = purposeCounts.Select((p, idx) => new VisitorPurposeItem
                {
                    Purpose = p.Purpose,
                    Count = p.Count,
                    Percentage = Math.Round((double)p.Count / totalCheckInsForPurpose * 100, 1),
                    ColorHex = idx switch
                    {
                        0 => "#3B82F6", // Blue
                        1 => "#10B981", // Green
                        2 => "#F59E0B", // Amber
                        _ => "#64748B"  // Slate
                    }
                }).OrderByDescending(p => p.Count).ToList();
                VisitPurposes = new ObservableCollection<VisitorPurposeItem>(purposeList);

                // Top Hosts (Teachers)
                var teacherIds = checkInLogs.Select(l => l.HostTeacherId).Distinct().ToList();
                var teacherDict = await db.TeacherProfiles
                    .Where(t => teacherIds.Contains(t.TeacherCode))
                    .ToDictionaryAsync(t => t.TeacherCode, t => t.DisplayName);

                var hostCounts = checkInLogs
                    .GroupBy(l => l.HostTeacherId)
                    .Select(g => new
                    {
                        TeacherName = teacherDict.ContainsKey(g.Key) ? teacherDict[g.Key] : (string.IsNullOrEmpty(g.Key) ? "Không có" : g.Key),
                        Count = g.Count()
                    })
                    .OrderByDescending(h => h.Count)
                    .Take(5)
                    .Select(h => new VisitorHostItem { TeacherName = h.TeacherName, Count = h.Count })
                    .ToList();
                TopHosts = new ObservableCollection<VisitorHostItem>(hostCounts);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load reports data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadAuditLogsAsync()
        {
            try
            {
                using var db = new AppDbContext();

                // Get master settings for integrity check mode
                var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Audit_IntegrityCheckMode");
                bool isStrict = setting == null || string.Equals(setting.Value, "Strict", StringComparison.OrdinalIgnoreCase);

                var query = db.AuditLogs.AsNoTracking();

                // 1. Text search
                if (!string.IsNullOrWhiteSpace(SearchAuditText))
                {
                    string text = SearchAuditText.Trim().ToLower();
                    query = query.Where(a => a.ActorName.ToLower().Contains(text) ||
                                             a.Action.ToLower().Contains(text) ||
                                             a.Details.ToLower().Contains(text));
                }

                // 2. Action Type filter
                if (SelectedAuditActionType != "Tất cả")
                {
                    switch (SelectedAuditActionType)
                    {
                        case "Cấu hình hệ thống":
                            query = query.Where(a => a.Action.Contains("UpdateSettings") || a.Action.Contains("SaveSetting") || a.Action.Contains("Update"));
                            break;
                        case "Thiết bị di động":
                            query = query.Where(a => a.Action.Contains("Mobile") || a.Action.Contains("RegisterDevice") || a.Action.Contains("ApproveDevice") || a.Action.Contains("RejectDevice"));
                            break;
                        case "Hành động công văn":
                            query = query.Where(a => a.Action.Contains("Document") || a.Action.Contains("Escalate") || a.Action.Contains("Upload"));
                            break;
                        case "Báo cáo sự cố":
                            query = query.Where(a => a.Action.Contains("Incident") || a.Action.Contains("Notify"));
                            break;
                        case "Khác":
                            query = query.Where(a => !a.Action.Contains("UpdateSettings") && !a.Action.Contains("SaveSetting") && !a.Action.Contains("Update") &&
                                                     !a.Action.Contains("Mobile") && !a.Action.Contains("RegisterDevice") && !a.Action.Contains("ApproveDevice") && !a.Action.Contains("RejectDevice") &&
                                                     !a.Action.Contains("Document") && !a.Action.Contains("Escalate") && !a.Action.Contains("Upload") &&
                                                     !a.Action.Contains("Incident") && !a.Action.Contains("Notify"));
                            break;
                    }
                }

                var list = await query.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync();

                // Calculate integrity map
                var integrityMap = AuditHelper.GetChainIntegrityMap();

                var displays = list.Select(a =>
                {
                    bool isValid = true;
                    if (isStrict)
                    {
                        // Check against the integrity map if present
                        if (integrityMap.TryGetValue(a.Id, out bool validVal))
                        {
                            isValid = validVal;
                        }
                    }

                    string statusText = "Hợp lệ";
                    string statusColor = "#16A34A"; // green

                    if (!isStrict)
                    {
                        statusText = "Không kiểm tra";
                        statusColor = "#4B5563"; // gray
                    }
                    else if (string.IsNullOrEmpty(a.RowHash))
                    {
                        statusText = "Bản ghi di sản";
                        statusColor = "#4B5563"; // gray
                    }
                    else if (!isValid)
                    {
                        statusText = "CẢNH BÁO: BỊ GIẢ MẠO!";
                        statusColor = "#DC2626"; // bright red
                    }

                    return new ReportsAuditLogDisplay
                    {
                        Id = a.Id,
                        Action = a.Action,
                        ActorName = a.ActorName,
                        Details = a.Details,
                        Timestamp = a.Timestamp,
                        IntegrityStatus = statusText,
                        IntegrityColor = statusColor
                    };
                }).ToList();

                AuditLogs = new ObservableCollection<ReportsAuditLogDisplay>(displays);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load audit logs");
            }
        }
    }
}
