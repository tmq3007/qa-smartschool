using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.HRModule.Views
{
    public partial class ContractView : Page
    {
        private readonly AppDbContext _db;

        public ContractView()
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
                if (!_db.Contracts.Any())
                {
                    _db.Contracts.AddRange(
                        new Contract { StaffId = 1, ContractNumber = "HD/2020/01", ContractType = "Indefinite", StartDate = new DateTime(2020, 8, 1), EndDate = null, Status = "Active" },
                        new Contract { StaffId = 2, ContractNumber = "HD/2021/05", ContractType = "1-Year", StartDate = new DateTime(2021, 5, 1), EndDate = new DateTime(2022, 5, 1), Status = "Expired" }
                    );
                    _db.SaveChanges();
                }

                DgContracts.ItemsSource = _db.Contracts.ToList();
            }
            catch (Exception ex) { Log.Warning("Contract Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CbStaff.ItemsSource = _db.TeacherProfiles.ToList();
                if (CbStaff.Items.Count > 0) CbStaff.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load staff list for contract: {Err}", ex.Message);
            }

            TxtContractNumber.Text = string.Empty;
            CbContractType.SelectedIndex = 0;
            DpStartDate.SelectedDate = DateTime.Today;
            DpEndDate.SelectedDate = DateTime.Today.AddYears(1);
            CbStatus.SelectedIndex = 0;
            PopupAddContract.Visibility = Visibility.Visible;
        }

        private void CbContractType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelEndDate == null) return;
            if (CbContractType.SelectedItem is ComboBoxItem item)
            {
                string type = item.Content?.ToString() ?? "";
                if (type == "Indefinite")
                {
                    PanelEndDate.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PanelEndDate.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnCancelAddContract_Click(object sender, RoutedEventArgs e)
        {
            PopupAddContract.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int staffId = 1;
                if (CbStaff.SelectedValue is int sId)
                {
                    staffId = sId;
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn nhân sự giáo viên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string contractNumber = TxtContractNumber.Text.Trim();
                string contractType = (CbContractType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Probation";
                DateTime startDate = DpStartDate.SelectedDate ?? DateTime.Today;
                DateTime? endDate = DpEndDate.SelectedDate;
                string status = (CbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Active";

                if (string.IsNullOrEmpty(contractNumber))
                {
                    MessageBox.Show("Vui lòng nhập số hợp đồng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (contractType != "Indefinite")
                {
                    if (endDate == null)
                    {
                        MessageBox.Show("Vui lòng nhập ngày kết thúc hợp đồng!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (endDate <= startDate)
                    {
                        MessageBox.Show("Ngày kết thúc phải sau ngày bắt đầu hợp đồng!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    endDate = null;
                }

                var contract = new Contract
                {
                    StaffId = staffId,
                    ContractNumber = contractNumber,
                    ContractType = contractType,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = status
                };

                _db.Contracts.Add(contract);
                _db.SaveChanges();

                PopupAddContract.Visibility = Visibility.Collapsed;
                LoadData();
                MessageBox.Show("✅ Đã tạo hợp đồng mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo hợp đồng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

