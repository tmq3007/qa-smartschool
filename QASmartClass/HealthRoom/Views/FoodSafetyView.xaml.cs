using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.HealthRoom.Views
{
    public partial class FoodSafetyView : Page
    {
        private readonly AppDbContext _db;

        public FoodSafetyView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadData();

            DpRecordDate.SelectedDateChanged += (s, ev) =>
            {
                if (DpRecordDate.SelectedDate is DateTime selDate)
                {
                    AutoFillMenu(selDate);
                }
            };
        }

        private void AutoFillMenu(DateTime date)
        {
            try
            {
                var menus = _db.SchoolMenus.Where(m => m.Date.Date == date.Date).ToList();
                if (menus.Any())
                {
                    TxtMenuItems.Text = string.Join(", ", menus.Select(m => m.Items));
                }
                else
                {
                    TxtMenuItems.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to auto-fill canteen menu: {Err}", ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                if (!_db.FoodSafetyRecords.Any())
                {
                    _db.FoodSafetyRecords.AddRange(
                        new FoodSafetyRecord { Date = DateTime.Today, MenuItems = "Cơm trắng, Thịt kho trứng, Canh rau ngót", SampleKept = true, Inspector = "Cô Yến Y tế", Result = "Pass" },
                        new FoodSafetyRecord { Date = DateTime.Today.AddDays(-1), MenuItems = "Bún chả cá, Trái cây tráng miệng", SampleKept = true, Inspector = "Cô Yến Y tế", Result = "Pass" }
                    );
                    _db.SaveChanges();
                }

                DgRecords.ItemsSource = _db.FoodSafetyRecords.OrderByDescending(r => r.Date).ToList();
            }
            catch (Exception ex) { Log.Warning("FoodSafety Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            DpRecordDate.SelectedDate = DateTime.Today;
            AutoFillMenu(DateTime.Today);
            ChkSampleKept.IsChecked = true;
            TxtInspector.Text = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Cô Yến Y tế";
            CbResult.SelectedIndex = 0;
            PopupAddRecord.Visibility = Visibility.Visible;
        }

        private void BtnCancelAddRecord_Click(object sender, RoutedEventArgs e)
        {
            PopupAddRecord.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveRecord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime date = DpRecordDate.SelectedDate ?? DateTime.Today;
                string menuItems = TxtMenuItems.Text.Trim();
                bool sampleKept = ChkSampleKept.IsChecked == true;
                string inspector = TxtInspector.Text.Trim();
                string result = (CbResult.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Pass";

                if (string.IsNullOrEmpty(menuItems))
                {
                    MessageBox.Show("Vui lòng nhập thực đơn trong ngày!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(inspector))
                {
                    MessageBox.Show("Vui lòng nhập họ tên người kiểm tra!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (date > DateTime.Today)
                {
                    MessageBox.Show("Ngày kiểm thực không được ở tương lai!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var record = new FoodSafetyRecord
                {
                    Date = date,
                    MenuItems = menuItems,
                    SampleKept = sampleKept,
                    Inspector = inspector,
                    Result = result
                };

                _db.FoodSafetyRecords.Add(record);
                _db.SaveChanges();

                PopupAddRecord.Visibility = Visibility.Collapsed;
                LoadData();
                MessageBox.Show("✅ Đã thêm nhật ký kiểm thực thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu nhật ký kiểm thực: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

