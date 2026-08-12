using QASmartClass.Data;
using System.Globalization;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.HRModule.Views
{
    public partial class AssetManagementView : Page
    {
        private readonly AppDbContext _db;

        public AssetManagementView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (!_db.Assets.Any())
                {
                    _db.Assets.AddRange(
                        new Asset { Name = "Máy chiếu Panasonic", Category = "Electronics", SerialNumber = "PN12345", PurchaseDate = DateTime.Today.AddYears(-2), Value = 15000000, Location = "Phòng 101", Status = "InUse", AssignedTo = "Nguyễn Văn A" },
                        new Asset { Name = "Laptop Dell Inspiron", Category = "Electronics", SerialNumber = "DL98765", PurchaseDate = DateTime.Today.AddYears(-3), Value = 12000000, Location = "Văn phòng", Status = "UnderRepair", AssignedTo = "Trần Thị B" }
                    );
                    _db.SaveChanges();
                }

                FilterData();
            }
            catch (Exception ex) { Log.Warning("Asset Load error: {Err}", ex.Message); }
        }

        private void FilterData()
        {
            var query = _db.Assets.AsQueryable();

            // Lọc trạng thái (giả lập UI logic)
            // Trong thực tế sẽ map từ RadioButton. Ở đây load toàn bộ cho nhanh.
            
            if (ChkExpiringWarranty.IsChecked == true)
            {
                // Giả lập tài sản mua quá 3 năm (sắp hết bảo hành)
                DateTime limit = DateTime.Today.AddYears(-3);
                query = query.Where(a => a.PurchaseDate <= limit);
            }

            DgAssets.ItemsSource = query.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) FilterData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            TxtAssetName.Text = string.Empty;
            CbAssetCategory.SelectedIndex = 0;
            TxtSerialNumber.Text = string.Empty;
            DpPurchaseDate.SelectedDate = DateTime.Today;
            TxtValue.Text = string.Empty;
            TxtLocation.Text = string.Empty;
            TxtAssignedTo.Text = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? string.Empty;
            PopupAddAsset.Visibility = Visibility.Visible;
        }

        private void BtnCancelAddAsset_Click(object sender, RoutedEventArgs e)
        {
            PopupAddAsset.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveAsset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = TxtAssetName.Text.Trim();
                string category = (CbAssetCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";
                string serial = TxtSerialNumber.Text.Trim();
                DateTime purchaseDate = DpPurchaseDate.SelectedDate ?? DateTime.Today;
                string valueStr = TxtValue.Text.Trim();
                string location = TxtLocation.Text.Trim();
                string assignedTo = TxtAssignedTo.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Vui lòng nhập tên tài sản!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(valueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value) || value < 0)
                {
                    MessageBox.Show("Nguyên giá tài sản phải là số lớn hơn hoặc bằng 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (purchaseDate > DateTime.Today)
                {
                    MessageBox.Show("Ngày mua không được ở tương lai!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var asset = new Asset
                {
                    Name = name,
                    Category = category,
                    SerialNumber = serial,
                    PurchaseDate = purchaseDate,
                    Value = value,
                    Location = location,
                    AssignedTo = assignedTo,
                    Status = "InUse"
                };

                _db.Assets.Add(asset);
                _db.SaveChanges();

                PopupAddAsset.Visibility = Visibility.Collapsed;
                LoadData();
                MessageBox.Show("✅ Đã thêm tài sản thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu tài sản: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int _selectedAssetIdForTransfer = 0;

        private void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (DgAssets.SelectedItem is Asset asset)
            {
                _selectedAssetIdForTransfer = asset.Id;
                TxtTransferTarget.Text = $"Tài sản: {asset.Name} (S/N: {asset.SerialNumber})";
                TxtFromDept.Text = asset.Location;
                TxtToDept.Text = string.Empty;
                DpTransferDate.SelectedDate = DateTime.Today;
                TxtTransferReason.Text = string.Empty;
                PopupTransferAsset.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Vui lòng chọn 1 tài sản để điều chuyển.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelTransfer_Click(object sender, RoutedEventArgs e)
        {
            PopupTransferAsset.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedAssetIdForTransfer <= 0) return;
                var asset = _db.Assets.Find(_selectedAssetIdForTransfer);
                if (asset == null) return;

                string fromDept = TxtFromDept.Text.Trim();
                string toDept = TxtToDept.Text.Trim();
                DateTime transferDate = DpTransferDate.SelectedDate ?? DateTime.Today;
                string reason = TxtTransferReason.Text.Trim();

                if (string.IsNullOrEmpty(toDept))
                {
                    MessageBox.Show("Vui lòng nhập nơi chuyển đến!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (transferDate > DateTime.Today)
                {
                    MessageBox.Show("Ngày điều chuyển không được ở tương lai!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Cập nhật vị trí tài sản
                asset.Location = toDept;
                
                // Lưu phiếu điều chuyển
                var transfer = new AssetTransfer
                {
                    AssetId = asset.Id,
                    FromDept = fromDept,
                    ToDept = toDept,
                    TransferDate = transferDate,
                    Reason = reason
                };

                _db.AssetTransfers.Add(transfer);
                _db.SaveChanges();

                PopupTransferAsset.Visibility = Visibility.Collapsed;
                LoadData();
                MessageBox.Show($"✅ Đã lập phiếu điều chuyển tài sản '{asset.Name}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lập phiếu điều chuyển: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt",
                    FileName = $"BaoCao_KiemKe_TaiSan_{DateTime.Now:yyyyMMdd}.txt"
                };

                if (sfd.ShowDialog() == true)
                {
                    var assets = _db.Assets.OrderBy(a => a.Name).ToList();
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("BÁO CÁO KIỂM KÊ TÀI SẢN CỐ ĐỊNH");
                        sb.AppendLine($"Ngày lập: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                        sb.AppendLine("=".PadRight(50, '='));
                        sb.AppendLine();
                        sb.AppendLine($"Tổng số tài sản: {assets.Count} thiết bị");
                        sb.AppendLine($"Tổng nguyên giá: {assets.Sum(a => a.Value):N0} VNĐ");
                        sb.AppendLine();
                        sb.AppendLine("DANH SÁCH CHI TIẾT:");
                        sb.AppendLine("Tên tài sản\tDanh mục\tSerial\tVị trí\tNgười chịu trách nhiệm\tNguyên giá\tTrạng thái");
                        foreach (var a in assets)
                        {
                            sb.AppendLine($"{a.Name}\t{a.Category}\t{a.SerialNumber}\t{a.Location}\t{a.AssignedTo}\t{a.Value:N0}\t{a.Status}");
                        }
                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    });
                    MessageBox.Show("✅ Xuất báo cáo kiểm kê thành công!", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}

