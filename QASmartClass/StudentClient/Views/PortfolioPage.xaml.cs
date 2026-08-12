using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.StudentClient.Views
{
    public partial class PortfolioPage : Page
    {
        private readonly AppDbContext _db;
        private readonly AppDbContext? _dbLocal;
        private readonly bool _ownsDb;
        private int _currentStudentId = 0;

        public PortfolioPage()
        {
            InitializeComponent();
            if (QASmartClass.Services.AppServices.Database != null)
            {
                _db = QASmartClass.Services.AppServices.Database;
                _ownsDb = false;
            }
            else
            {
                _dbLocal = new AppDbContext();
                _db = _dbLocal;
                _ownsDb = true;
            }
            Loaded += Page_Loaded;
            Unloaded += (s, e) => { if (_ownsDb) _dbLocal?.Dispose(); };
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
            _currentStudentId = identityService.GetCurrentStudent().Id;
            if (_currentStudentId <= 0) { MessageBox.Show("Vui lòng đăng nhập!"); return; }
            LoadPortfolio();
        }

        private void LoadPortfolio()
        {
            try
            {
                var items = _db.PortfolioItems.Where(p => p.StudentId == _currentStudentId).ToList()
                    .Select(p => new { p.Id, p.Title, p.Description, p.Category, p.CreatedAt, CreatedAtDisplay = $"Ngày tải lên: {p.CreatedAt:dd/MM/yyyy}" }).ToList();

                LvProducts.ItemsSource = items.Where(p => p.Category == "Product").ToList();
                LvAchievements.ItemsSource = items.Where(p => p.Category == "Achievement").ToList();
                LvCertificates.ItemsSource = items.Where(p => p.Category == "Certificate").ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải dữ liệu Portfolio.");
            }
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            TxtTitle.Text = string.Empty;
            CbCategory.SelectedIndex = 0;
            TxtFilePath.Text = string.Empty;
            TxtDescription.Text = string.Empty;
            PopupAddItem.Visibility = Visibility.Visible;
        }

        private void BtnCancelAddItem_Click(object sender, RoutedEventArgs e)
        {
            PopupAddItem.Visibility = Visibility.Collapsed;
        }

        private void BtnBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*",
                Title = "Chọn tệp đính kèm sản phẩm"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtFilePath.Text = dlg.FileName;
            }
        }

        private void BtnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = TxtTitle.Text.Trim();
                string category = (CbCategory.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Product";
                string filePath = TxtFilePath.Text.Trim();
                string description = TxtDescription.Text.Trim();

                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Vui lòng nhập tiêu đề sản phẩm/thành tích!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var item = new PortfolioItem
                {
                    StudentId = _currentStudentId,
                    Title = title,
                    Category = category,
                    FilePath = filePath,
                    Description = description,
                    CreatedAt = DateTime.Now
                };

                _db.PortfolioItems.Add(item);
                _db.SaveChanges();

                PopupAddItem.Visibility = Visibility.Collapsed;
                LoadPortfolio();
                MessageBox.Show("✅ Đã thêm sản phẩm học tập vào E-Portfolio!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu sản phẩm học tập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int itemId)
            {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa mục này không?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var item = _db.PortfolioItems.Find(itemId);
                        if (item != null)
                        {
                            _db.PortfolioItems.Remove(item);
                            _db.SaveChanges();
                            LoadPortfolio();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Lỗi khi xoá PortfolioItem {Id}", itemId);
                    }
                }
            }
        }
    }
}

