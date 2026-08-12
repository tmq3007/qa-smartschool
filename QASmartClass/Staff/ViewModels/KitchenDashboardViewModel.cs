using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using QASmartClass.Staff.Services;

namespace QASmartClass.Staff.ViewModels
{
    public partial class KitchenDashboardViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<SchoolMenuDisplay> _menus = new();
        [ObservableProperty] private ObservableCollection<FoodAllergy> _allergies = new();
        
        [ObservableProperty] private string _mealCount = "Đang tính toán...";
        [ObservableProperty] private string _menuItems = string.Empty;
        [ObservableProperty] private string _allergens = string.Empty;
        [ObservableProperty] private DateTime? _menuDate = DateTime.Today;
        [ObservableProperty] private string _selectedMealType = "Lunch";
        [ObservableProperty] private string _nutritionInfo = "Đầy đủ dưỡng chất";

        [ObservableProperty] private bool _isMenuVisible = true;
        [ObservableProperty] private bool _isAllergyVisible = false;

        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private bool _isColdStorageWidgetVisible = true;
        [ObservableProperty] private bool _isColdStorageViolation = false;
        [ObservableProperty] private string _coldStorageTempString = "Chưa có dữ liệu";
        [ObservableProperty] private string _coldStorageTime = "--:--";

        public KitchenDashboardViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await LoadExpectedMealsAsync();
                await LoadMenusAsync();
                await LoadColdStorageStatusAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnMenuDateChanged(DateTime? value)
        {
            _ = LoadMenusAsync();
            _ = LoadExpectedMealsAsync();
            _ = LoadColdStorageStatusAsync();
        }

        private async Task LoadExpectedMealsAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var formulaSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_MealCalculationFormula");
                string formula = formulaSetting?.Value ?? "1"; // Default to preorder-based (standard)

                var targetDate = MenuDate ?? DateTime.Today;

                if (formula == "0")
                {
                    // Attendance-only
                    var service = new KitchenService(db);
                    int meals = await service.GetEstimatedMealsAsync(targetDate);
                    MealCount = $"{meals} Suất (Dựa trên điểm danh)";
                }
                else if (formula == "1")
                {
                    // Preorder-based (Total active - approved leaves + present staff)
                    var startDate = targetDate.Date;
                    var endDate = startDate.AddDays(1);
                    var cancelledToday = await db.StudentLeaveRequests
                        .CountAsync(r => r.LeaveDate >= startDate && r.LeaveDate < endDate && r.Status == "Approved");
                    var addedToday = await db.StaffAttendances
                        .CountAsync(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");
                    var totalStudents = await db.Students.CountAsync(s => s.Status == "Active");
                    var finalMeals = totalStudents - cancelledToday + addedToday;

                    MealCount = $"{finalMeals} Suất (Báo hủy: -{cancelledToday}, Báo bù: +{addedToday})";
                }
                else
                {
                    // Hybrid
                    var startDate = targetDate.Date;
                    var endDate = startDate.AddDays(1);
                    int presentCount = await db.AttendanceRecords
                        .CountAsync(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");

                    if (presentCount > 0)
                    {
                        MealCount = $"{presentCount} Suất (Điểm danh thực tế)";
                    }
                    else
                    {
                        var cancelledToday = await db.StudentLeaveRequests
                            .CountAsync(r => r.LeaveDate >= startDate && r.LeaveDate < endDate && r.Status == "Approved");
                        var addedToday = await db.StaffAttendances
                            .CountAsync(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");
                        var totalStudents = await db.Students.CountAsync(s => s.Status == "Active");
                        var finalMeals = totalStudents - cancelledToday + addedToday;

                        MealCount = $"{finalMeals} Suất (Dự kiến: Sĩ số {totalStudents} - Vắng {cancelledToday} + GV {addedToday})";
                    }
                }
            }
            catch (Exception ex)
            {
                MealCount = "Lỗi: " + ex.Message;
                Serilog.Log.Error(ex, "[KitchenDashboard] LoadExpectedMeals failed");
            }
        }

        private async Task LoadColdStorageStatusAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var enableSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_EnableColdStorageWidget");
                IsColdStorageWidgetVisible = enableSetting?.Value.ToLower() == "true";

                if (!IsColdStorageWidgetVisible) return;

                var latestLog = await db.KitchenColdStorageLogs
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                if (latestLog != null)
                {
                    IsColdStorageViolation = latestLog.IsViolation;
                    ColdStorageTempString = $"{latestLog.Temperature:F1}°C (Ẩm: {latestLog.Humidity:F0}%)";
                    ColdStorageTime = latestLog.Timestamp.ToString("HH:mm");
                }
                else
                {
                    IsColdStorageViolation = false;
                    ColdStorageTempString = "Chưa có dữ liệu";
                    ColdStorageTime = "--:--";
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[KitchenDashboard] LoadColdStorageStatus failed");
            }
        }

        [RelayCommand]
        private async Task LoadMenusAsync()
        {
            IsMenuVisible = true;
            IsAllergyVisible = false;
            try
            {
                using var db = StaffDbFactory.Create();
                var service = new KitchenService(db);
                var data = await service.GetMenusByDateAsync(MenuDate ?? DateTime.Today);
                var mapped = data.Select(m => new SchoolMenuDisplay
                {
                    Id = m.Id,
                    Date = m.Date,
                    MealType = m.MealType,
                    Items = m.Items,
                    NutritionInfo = m.NutritionInfo,
                    Allergens = m.Allergens
                });
                Menus = new ObservableCollection<SchoolMenuDisplay>(mapped);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[KitchenDashboard] LoadMenus failed");
            }
        }

        [RelayCommand]
        private async Task LoadAllergiesAsync()
        {
            IsMenuVisible = false;
            IsAllergyVisible = true;
            try
            {
                using var db = StaffDbFactory.Create();
                var service = new KitchenService(db);
                var data = await service.GetAllergiesAsync();
                Allergies = new ObservableCollection<FoodAllergy>(data);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[KitchenDashboard] LoadAllergies failed");
            }
        }

        [RelayCommand]
        private async Task SaveMenuAsync()
        {
            if (MenuDate.HasValue && MenuDate.Value.Date < DateTime.Today)
            {
                await AppServices.UIService.ShowInfoAsync("Không thể thiết lập thực đơn cho ngày trong quá khứ.", "Lỗi ngày tháng");
                return;
            }

            if (string.IsNullOrWhiteSpace(MenuItems))
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập danh sách món ăn.", "Lỗi");
                return;
            }

            try
            {
                var menu = new SchoolMenu
                {
                    Date = MenuDate ?? DateTime.Today,
                    MealType = SelectedMealType,
                    Items = MenuItems,
                    NutritionInfo = string.IsNullOrWhiteSpace(NutritionInfo) ? "Đầy đủ dưỡng chất" : NutritionInfo.Trim(),
                    Allergens = Allergens
                };

                using (var db = StaffDbFactory.Create())
                {
                    var existingMenu = await db.SchoolMenus.FirstOrDefaultAsync(m => m.Date.Date == menu.Date.Date && m.MealType == menu.MealType);
                    if (existingMenu != null)
                    {
                        existingMenu.Items = menu.Items;
                        existingMenu.NutritionInfo = menu.NutritionInfo;
                        existingMenu.Allergens = menu.Allergens;
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        var service = new KitchenService(db);
                        await service.AddMenuAsync(menu);
                    }
                }

                using (var auditDb = StaffDbFactory.Create())
                {
                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Bếp/Cấp dưỡng";
                    QASmartClass.Services.AuditHelper.Log(auditDb, "Save_SchoolMenu", actor, $"Saved school menu for date {menu.Date:dd/MM/yyyy} ({menu.MealType}): {menu.Items}");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var dbNotification = StaffDbFactory.Create();
                        var mobileApi = new MobileApiService(dbNotification);
                        string friendlyMeal = menu.MealType switch {
                            "Breakfast" => "Bữa sáng",
                            "Lunch" => "Bữa trưa",
                            "Snack" => "Bữa phụ/nhẹ",
                            "Dinner" => "Bữa tối",
                            _ => menu.MealType
                        };
                        string title = $"Thực đơn mới ngày {menu.Date:dd/MM}";
                        string body = $"Nhà trường cập nhật thực đơn {friendlyMeal}: {menu.Items}. Dinh dưỡng: {menu.NutritionInfo}.";
                        await mobileApi.SendPushNotificationAsync(0, "Parent", title, body, "General");
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "[KitchenDashboard] Auto push menu notification failed");
                    }
                });
                
                MenuItems = string.Empty;
                Allergens = string.Empty;
                NutritionInfo = "Đầy đủ dưỡng chất";
                
                await LoadMenusAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[KitchenDashboard] SaveMenu failed");
                await AppServices.UIService.ShowInfoAsync("Lỗi khi lưu thực đơn.", "Lỗi");
            }
        }

        [RelayCommand]
        private async Task PreorderAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var targetDate = MenuDate ?? DateTime.Today;
                var startDate = targetDate.Date;
                var endDate = startDate.AddDays(1);
 
                // Báo hủy: Số học sinh có đơn xin nghỉ phép được phê duyệt trong ngày mục tiêu
                var cancelledToday = await db.StudentLeaveRequests
                    .CountAsync(r => r.LeaveDate >= startDate && r.LeaveDate < endDate && r.Status == "Approved");
 
                // Báo bù: Số giáo viên/nhân viên chấm công có mặt ngày mục tiêu
                var addedToday = await db.StaffAttendances
                    .CountAsync(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");
 
                var totalStudents = await db.Students.CountAsync(s => s.Status == "Active");
                var finalMeals = totalStudents - cancelledToday + addedToday;
 
                await AppServices.UIService.ShowInfoAsync($"Đồng bộ App thành công ngày {targetDate:dd/MM/yyyy}!\n\n- Suất học sinh báo hủy (nghỉ phép): -{cancelledToday} suất\n- Suất giáo viên báo bù (có mặt): +{addedToday} suất\n\nTổng suất dự kiến: {finalMeals} suất ăn.", "Đồng bộ thành công");
                
                // Recalculate MealCount based on the actual formula configuration
                await LoadExpectedMealsAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[KitchenDashboard] Preorder failed");
                MealCount = "Lỗi khi tính số suất";
            }
        }
    }

    public class SchoolMenuDisplay
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; } = string.Empty;
        public string Items { get; set; } = string.Empty;
        public string NutritionInfo { get; set; } = string.Empty;
        public string Allergens { get; set; } = string.Empty;

        public string FriendlyMealType => MealType switch
        {
            "Breakfast" => "Bữa sáng",
            "Lunch" => "Bữa trưa",
            "Snack" => "Bữa phụ/nhẹ",
            "Dinner" => "Bữa tối",
            _ => MealType
        };
    }
}

