using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QASmartClass.Staff.Views
{
    public partial class StaffDashboardWindow : Window
    {
        private Button? _activeNavButton;
        private DispatcherTimer? _clockTimer;
        private bool _isLoggingOut = false;

        public StaffDashboardWindow()
        {
            InitializeComponent();
            StartClock();
            // Set default active button
            if (btnOverview != null)
                _activeNavButton = btnOverview;
                
            Loaded += (s, e) => {
                UpdateUserInfo();
                ApplyRolePermissions();
                CheckDatabaseSizeAlert();
            };

            Closing += (s, e) => {
                if (!_isLoggingOut)
                {
                    // User đóng bằng nút X → KHÔNG close, chỉ Hide + chuyển mode
                    e.Cancel = true;
                    try
                    {
                        var app = Application.Current as QASmartTouch.App;
                        if (app != null)
                        {
                            app.ModeService.GoToClass(); // Giữ session, chỉ chuyển mode
                        }
                        else
                        {
                            Hide();
                        }
                    }
                    catch { Hide(); }
                    return;
                }

                // Vòng 3: Dispose và dọn dẹp cache để tránh rò rỉ DbContext
                foreach (var view in _viewCache.Values)
                {
                    if (view is System.Windows.Controls.UserControl uc && uc.DataContext is IDisposable disposableVm)
                    {
                        disposableVm.Dispose();
                    }
                    else if (view is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _viewCache.Clear();

                if (DataContext is IDisposable disposableDc)
                {
                    disposableDc.Dispose();
                }

                _clockTimer?.Stop();
                QASmartClass.Staff.Services.StaffSession.Logout();

                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app != null)
                    {
                        app._staffShell = null;
                        app.ModeService.GoToClass();
                    }
                }
                catch { }
            };
        }

        private void ApplyRolePermissions()
        {
            var role = QASmartClass.Staff.Services.StaffSession.Role;
            
            var user = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            // Fix RBAC: lookup YouthMember position from DB instead of comparing TeacherProfile.Role
            bool isYouthUnion = false;
            try
            {
                string fullName = user?.FullName;
                using var _dbRbac = new QASmartClass.Data.AppDbContext();
                var youthMember = _dbRbac.YouthMembers.FirstOrDefault(m => m.StudentName == fullName && m.Status == "Active");
                isYouthUnion = QASmartClass.Staff.Services.StaffSession.CanManageYouthUnion(youthMember?.Position ?? "");
            }
            catch { /* Fallback: no youth access */ }

            // Dynamic RBAC from Database
            System.Collections.Generic.List<string> allowedTags = new();
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                allowedTags = db.RolePermissions
                    .Where(rp => rp.Role == role)
                    .Select(rp => rp.PermissionTag)
                    .ToList();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load role permissions from database");
            }

            // Common features for all staff (chỉ giữ các tiện ích cá nhân mà mọi nhân viên đều cần)
            var commonFeatures = new[] { "event_calendar", "Incidents", "task_management", "eoffice_routing", "hr_profile", "hr_contract", "hr_leave", "hr_attendance" };

            if (navMenu == null) return;

            ApplyPermissionsToControl(navMenu, role, allowedTags, isYouthUnion, commonFeatures);

            // Điều hướng đến trang đích mặc định phù hợp với Role
            NavigateToDefaultLandingPage();
        }

        private bool ApplyPermissionsToControl(object parent, string role, System.Collections.Generic.List<string> allowedTags, bool isYouthUnion, string[] commonFeatures)
        {
            if (parent == null) return false;

            if (parent is Button btn && btn.Tag is string feature)
            {
                bool hasAccess = false;
                if (role == "Admin")
                {
                    // Admin có toàn quyền 100% tất cả chức năng
                    hasAccess = true;
                }
                else if (role == "HieuTruong")
                {
                    // Hiệu trưởng: Quyền tối cao bao trùm toàn bộ quyền của Hiệu phó, 
                    // cộng thêm các quyền chiến lược vĩ mô (Overview, moet, reports, payroll, tuition, assets, push_notification).
                    // Chỉ các chức năng cấu hình bảo mật kỹ thuật CSDL dành riêng cho Admin IT.
                    bool isTechnicalAdminOnly = feature is "role_manager" or "backup_restore" or "system_settings" or "system_audit";
                    hasAccess = !isTechnicalAdminOnly;
                }
                else if (role == "HieuPho")
                {
                    // Hiệu phó: Toàn quyền Chuyên môn dạy học, duyệt đơn nghỉ phép, sự cố, tổ chuyên môn...
                    // Ẩn Bảng lương (payroll), Báo cáo MOET (moet) và các chức năng kỹ thuật Admin IT.
                    bool isHpForbidden = feature is "payroll" or "moet" or "system_settings" or "role_manager" or "backup_restore" or "system_audit";
                    hasAccess = !isHpForbidden && (Array.IndexOf(commonFeatures, feature) >= 0 || allowedTags.Contains(feature) || (isYouthUnion && feature.StartsWith("youth_")));
                }
                else
                {
                    // Chặn triệt để các báo cáo vĩ mô và tính năng kỹ thuật đối với Giáo viên và các role tác nghiệp
                    bool isMacroAdminOnly = feature is "Overview" or "moet" or "app_analytics" or "system_settings" or "role_manager" or "backup_restore" or "system_audit" or "data_exporter" or "reports" or "payroll" or "tuition" or "assets";

                    if (role == "GV" && isMacroAdminOnly)
                    {
                        hasAccess = false;
                    }
                    else
                    {
                        if (Array.IndexOf(commonFeatures, feature) >= 0)
                            hasAccess = true;
                        if (allowedTags.Contains(feature))
                            hasAccess = true;
                        if (isYouthUnion && feature.StartsWith("youth_"))
                            hasAccess = true;
                    }
                }

                btn.Visibility = hasAccess ? Visibility.Visible : Visibility.Collapsed;
                return hasAccess;
            }

            bool anyChildVisible = false;

            if (parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (ApplyPermissionsToControl(child, role, allowedTags, isYouthUnion, commonFeatures))
                    {
                        anyChildVisible = true;
                    }
                }
            }
            else if (parent is Border border)
            {
                anyChildVisible = ApplyPermissionsToControl(border.Child, role, allowedTags, isYouthUnion, commonFeatures);
            }
            else if (parent is Expander expander)
            {
                anyChildVisible = ApplyPermissionsToControl(expander.Content, role, allowedTags, isYouthUnion, commonFeatures);
                expander.Visibility = anyChildVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (parent is ContentControl cc)
            {
                anyChildVisible = ApplyPermissionsToControl(cc.Content, role, allowedTags, isYouthUnion, commonFeatures);
            }

            return anyChildVisible;
        }

        private void UpdateUserInfo()
        {
            var user = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            if (user != null)
            {
                if (txtCardUserName != null) txtCardUserName.Text = $"{user.FullName} • {user.Role}";
                if (txtUserName != null) txtUserName.Text = user.FullName;
                if (txtUserRole != null) txtUserRole.Text = user.Role;
                if (txtUserInitials != null) 
                {
                    var names = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    txtUserInitials.Text = names.Length > 0 ? names[^1].Substring(0, 1).ToUpper() : "U";
                }
            }
        }

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        private void UpdateClock()
        {
            if (txtClock != null)
                txtClock.Text = DateTime.Now.ToString("HH:mm");
        }

        private void SetActiveNav(Button btn, string pageTitle)
        {
            // Reset previous active button
            if (_activeNavButton != null)
            {
                try { _activeNavButton.Style = FindResource("StaffNavButton") as Style; }
                catch { /* ignore if style not found */ }
            }

            // Set new active
            try { btn.Style = FindResource("StaffNavActive") as Style; }
            catch { /* ignore if style not found */ }
            _activeNavButton = btn;

            // Update top bar page title
            if (txtPageTitle != null)
                txtPageTitle.Text = pageTitle;
        }

        // ═══ FADE-IN ANIMATION ═══
        private void AnimateContentIn(object content)
        {
            if (content is UIElement element)
            {
                element.Opacity = 0;
                var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                element.BeginAnimation(UIElement.OpacityProperty, anim);
            }
        }

        // ═══ NAVIGATION MAP ═══
        private static readonly System.Collections.Generic.Dictionary<string, (string Icon, Func<object> Factory)> _viewMap = new()
        {
            ["Incidents"] = ("⚠", () => new QASmartClass.Staff.Views.IncidentManagementView()),
            ["counseling"] = ("🧠", () => new QASmartClass.Counseling.Views.SchoolCounselingView()),
            ["health"] = ("🏥", () => new QASmartClass.HealthRoom.Views.HealthRecordView()),
            ["epidemic"] = ("🦠", () => new QASmartClass.HealthRoom.Views.EpidemicMonitorView()),
            ["food_safety"] = ("🍱", () => new QASmartClass.HealthRoom.Views.FoodSafetyView()),
            ["hr_profile"] = ("👥", () => new QASmartClass.HRModule.Views.StaffProfileView()),
            ["department_mgmt"] = ("🏫", () => new QASmartClass.Leadership.Views.DepartmentManagementView()),
            ["award_mgmt"] = ("🎖", () => new QASmartClass.Leadership.Views.AwardManagementView()),
            ["hr_contract"] = ("📄", () => new QASmartClass.HRModule.Views.ContractView()),
            ["hr_leave"] = ("📅", () => new QASmartClass.HRModule.Views.LeaveRequestView()),
            ["hr_attendance"] = ("⏱", () => new QASmartClass.HRModule.Views.AttendanceStaffView()),
            ["assets"] = ("📦", () => new QASmartClass.Staff.Views.SchoolAssetManagementView()),
            ["tuition"] = ("💳", () => new QASmartClass.HRModule.Views.TuitionFeeView()),
            ["document_manager"] = ("📂", () => new QASmartClass.Staff.Views.DocumentManagerView()),
            ["payroll"] = ("💰", () => new QASmartClass.Staff.Views.PayrollView()),
            ["security"] = ("👮", () => new QASmartClass.Staff.Views.SecurityKioskView()),
            ["janitor"] = ("🧹", () => new QASmartClass.Staff.Views.CleaningScheduleView()),
            ["kitchen"] = ("🍽", () => new QASmartClass.Staff.Views.KitchenDashboardView()),
            ["medical_inventory"] = ("💊", () => new QASmartClass.HealthRoom.Views.MedicalInventoryView()),
            ["emergency"] = ("🚑", () => new QASmartClass.HealthRoom.Views.EmergencyReportView()),
            ["moet"] = ("📄", () => new QASmartClass.Staff.Views.MoetReportView()),
            ["reports"] = ("📊", () => new QASmartClass.Staff.Views.ReportsView()),
            ["library"] = ("📚", () => new SmartLibrary.Desktop.Views.Librarian.LibrarianDashboard()),
            ["mobile_app"] = ("📱", () => new QASmartClass.Staff.Views.MobileAppManagementView()),
            ["leave_request"] = ("📝", () => new QASmartClass.Staff.Views.LeaveRequestManagementView()),
            ["push_notification"] = ("🔔", () => new QASmartClass.Staff.Views.PushNotificationCenterView()),
            ["app_analytics"] = ("📈", () => new QASmartClass.Leadership.Views.AppUsageAnalyticsView()),
            ["task_management"] = ("📋", () => new QASmartClass.Staff.Views.TaskManagementView()),
            ["event_calendar"] = ("📆", () => new QASmartClass.Leadership.Views.SchoolEventCalendarView()),
            ["staff_performance"] = ("🎯", () => new QASmartClass.Leadership.Views.StaffPerformanceTrackerView()),
            ["eoffice_routing"] = ("📄", () => new QASmartClass.Staff.Views.DocumentRoutingView()),
            ["system_settings"] = ("⚙", () => new QASmartClass.Leadership.Views.SystemSettingsView()),
            ["role_manager"] = ("🔐", () => new QASmartClass.Leadership.Views.RolePermissionManagerView()),
            ["backup_restore"] = ("💽", () => new QASmartClass.Leadership.Views.BackupRestoreView()),
            ["system_audit"] = ("📜", () => new QASmartClass.Leadership.Views.SystemAuditLogView()),
            ["data_exporter"] = ("📑", () => new QASmartClass.Leadership.Views.SystemDataExporterView()),
            ["youth_dashboard"] = ("📈", () => new QASmartClass.YouthUnion.Views.YouthUnionDashboard()),
            ["youth_members"] = ("👥", () => new QASmartClass.YouthUnion.Views.MemberListView()),
            ["youth_recruitment"] = ("🤝", () => new QASmartClass.YouthUnion.Views.RecruitmentView()),
            ["youth_attendance"] = ("✅", () => new QASmartClass.YouthUnion.Views.AttendanceView()),
            ["youth_activities"] = ("⭐", () => new QASmartClass.YouthUnion.Views.ActivityManagementView()),
            ["youth_eventreg"] = ("📝", () => new QASmartClass.YouthUnion.Views.EventRegistrationView()),
            ["youth_emulation"] = ("🏆", () => new QASmartClass.YouthUnion.Views.EmulationScoreView()),
            ["youth_award"] = ("🎖", () => new QASmartClass.YouthUnion.Views.AwardProposalView()),
            ["youth_fee"] = ("💰", () => new QASmartClass.YouthUnion.Views.FeeTrackerView()),
            ["youth_voting"] = ("🗳", () => new QASmartClass.YouthUnion.Views.VotingView()),
            ["youth_document"] = ("📁", () => new QASmartClass.YouthUnion.Views.DocumentView()),
            ["youth_planbudget"] = ("📊", () => new QASmartClass.YouthUnion.Views.PlanBudgetView())
        };

        // ═══ VIEW CACHE ═══
        private readonly System.Collections.Generic.Dictionary<string, object> _viewCache = new();

        // ═══ NAVIGATION ═══
        private void NavFeature_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string feature)
            {
                if (_viewMap.TryGetValue(feature, out var entry))
                {
                    string title = (btn.Content is DockPanel dp && dp.Children.Count >= 2 && dp.Children[1] is TextBlock tb) ? tb.Text : feature;
                    SetActiveNav(btn, $"{entry.Icon} {title}");
                    FeatureFrame.Visibility = Visibility.Visible;

                    // Kiểm tra cache trước khi tạo mới
                    if (!_viewCache.TryGetValue(feature, out var view))
                    {
                        view = entry.Factory();
                        _viewCache[feature] = view;
                        Serilog.Log.Debug("View cache STORE: {Feature}", feature);
                    }
                    else
                    {
                        Serilog.Log.Debug("View cache HIT: {Feature}", feature);
                    }

                    // Disconnect previous logical child before setting new content
                    // to avoid "Specified element is already the logical child" error
                    FeatureFrame.Content = null;
                    FeatureFrame.Content = view;
                    AnimateContentIn(view);

                    bool canSeeOverview = btnOverview != null && btnOverview.Visibility == Visibility.Visible;
                    if (btnBackToOverview != null) btnBackToOverview.Visibility = canSeeOverview ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        // ═══ LOGOUT ═══
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            bool confirmLogout = true;
            if (!isTestHost)
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn đăng xuất khỏi tài khoản {QASmartClass.Staff.Services.StaffSession.DisplayName}?",
                    "⚠ Xác nhận đăng xuất",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                confirmLogout = (result == MessageBoxResult.Yes);
            }

            if (confirmLogout)
            {
                _isLoggingOut = true; // Flag để Closing handler biết cần logout thực sự
                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app != null)
                    {
                        app._staffShell = null;
                        app.ModeService.GoToClass();
                    }
                }
                catch { }
                Close(); // Closing handler sẽ gọi StaffSession.Logout()
            }
        }

        // ═══ INDIRECT FEATURE NAVIGATION (V17) ═══
        private Button FindButtonByTag(object parent, string featureTag)
        {
            if (parent == null) return null;
            if (parent is Button btn && btn.Tag?.ToString() == featureTag)
            {
                return btn;
            }
            if (parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindButtonByTag(child, featureTag);
                    if (found != null) return found;
                }
            }
            else if (parent is Border border)
            {
                var found = FindButtonByTag(border.Child, featureTag);
                if (found != null) return found;
            }
            else if (parent is ContentControl cc)
            {
                var found = FindButtonByTag(cc.Content, featureTag);
                if (found != null) return found;
            }
            return null;
        }

        public void NavigateToFeature(string featureTag)
        {
            if (string.IsNullOrEmpty(featureTag)) return;
            Button targetButton = FindButtonByTag(navMenu, featureTag);
            if (targetButton == null) return;

            if (targetButton.Command != null)
            {
                if (FeatureFrame != null)
                {
                    FeatureFrame.Content = null;
                    FeatureFrame.Visibility = Visibility.Hidden;
                }
                if (btnBackToOverview != null)
                {
                    btnBackToOverview.Visibility = Visibility.Collapsed;
                }

                targetButton.Command.Execute(null);

                string title = featureTag;
                if (targetButton.Content is DockPanel dp && dp.Children.Count >= 2 && dp.Children[1] is TextBlock tb)
                {
                    title = tb.Text;
                }
                SetActiveNav(targetButton, title);
            }
            else
            {
                NavFeature_Click(targetButton, new RoutedEventArgs());
            }
        }

        // ═══ BACK TO OVERVIEW / DEFAULT LANDING ═══
        private void BtnBackToOverview_Click(object sender, RoutedEventArgs e)
        {
            Button? btnOver = FindButtonByTag(navMenu, "Overview");
            if (btnOver != null && btnOver.Visibility == Visibility.Visible)
            {
                FeatureFrame.Content = null;
                FeatureFrame.Visibility = Visibility.Hidden;
                if (btnBackToOverview != null) btnBackToOverview.Visibility = Visibility.Collapsed;
                if (txtPageTitle != null) txtPageTitle.Text = "📊 Tổng quan";

                // Reset active nav to Overview
                SetActiveNav(btnOver, "📊 Tổng quan");
            }
            else
            {
                NavigateToDefaultLandingPage();
            }
        }

        private string GetDefaultFeatureForRole(string role)
        {
            return role switch
            {
                "Admin" or "HieuTruong" => "Overview",
                "HieuPho" => "leave_request",
                "GV" => "event_calendar",
                "BaoVe" => "Gate",
                "YTe" => "health",
                "LaoCong" => "janitor",
                "Bep" => "kitchen",
                "Ketoan" or "ThuQuy" => "tuition",
                "Counselor" => "counseling",
                "Librarian" => "library",
                _ => "event_calendar"
            };
        }

        private void NavigateToDefaultLandingPage()
        {
            var role = QASmartClass.Staff.Services.StaffSession.Role;
            string defaultFeature = GetDefaultFeatureForRole(role);

            Button? btn = FindButtonByTag(navMenu, defaultFeature);
            if (btn != null && btn.Visibility == Visibility.Visible)
            {
                NavigateToFeature(defaultFeature);
            }
            else
            {
                Button? firstVisible = FindFirstVisibleNavButton(navMenu);
                if (firstVisible != null && firstVisible.Tag is string tag)
                {
                    NavigateToFeature(tag);
                }
            }
        }

        private Button? FindFirstVisibleNavButton(object parent)
        {
            if (parent == null) return null;
            if (parent is Button btn && btn.Visibility == Visibility.Visible && btn.Tag != null)
            {
                return btn;
            }
            if (parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindFirstVisibleNavButton(child);
                    if (found != null) return found;
                }
            }
            else if (parent is Border border)
            {
                return FindFirstVisibleNavButton(border.Child);
            }
            else if (parent is Expander expander && expander.Visibility == Visibility.Visible)
            {
                return FindFirstVisibleNavButton(expander.Content);
            }
            else if (parent is ContentControl cc)
            {
                return FindFirstVisibleNavButton(cc.Content);
            }
            return null;
        }

        private void CheckDatabaseSizeAlert()
        {
            var role = QASmartClass.Staff.Services.StaffSession.Role;
            bool isAdmin = role == "Admin";
            if (!isAdmin)
            {
                if (brdDbWarning != null) brdDbWarning.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app != null && app.Database != null)
                {
                    var service = new QASmartClass.Services.DbSizeCheckService(app.Database);
                    var status = service.CheckDbSize();
                    if (status.IsOverLimit)
                    {
                        if (status.Action == "AutoClean")
                        {
                            // Tự động dọn dẹp ngầm
                            var retentionService = new QASmartClass.Services.DataRetentionService(app.Database);
                            retentionService.CleanOldData(90, 30);
                            
                            // Ghi AuditLog tự động dọn dẹp
                            app.Database.SystemSettings.Add(new QASmartClass.Data.SystemSetting
                            {
                                Id = "SystemSettings_AutoClean_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                                Value = "Database size exceeded " + status.LimitMb + "MB. Auto-cleanup run successfully.",
                                Category = "System",
                                LastUpdated = DateTime.Now
                            });
                            app.Database.SaveChanges();
                            
                            if (brdDbWarning != null) brdDbWarning.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // WarnOnly
                            if (brdDbWarning != null)
                            {
                                brdDbWarning.Visibility = Visibility.Visible;
                                txtDbWarningMsg.Text = $"⚠️ Cảnh báo IT: Cơ sở dữ liệu hiện tại đạt {status.CurrentSizeMb:F2} MB, vượt quá ngưỡng cho phép {status.LimitMb} MB. Vui lòng dọn dẹp dữ liệu hoặc sao lưu!";
                            }
                        }
                    }
                    else
                    {
                        if (brdDbWarning != null) brdDbWarning.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Lỗi kiểm tra kích thước cơ sở dữ liệu");
            }
        }

        private void BtnCleanDbNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app != null && app.Database != null)
                {
                    var retentionService = new QASmartClass.Services.DataRetentionService(app.Database);
                    retentionService.CleanOldData(90, 30);
                    
                    MessageBox.Show("Tiến trình dọn dẹp dữ liệu đã hoàn tất thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Kiểm tra lại sau khi dọn dẹp
                    CheckDatabaseSizeAlert();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi dọn dẹp dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
