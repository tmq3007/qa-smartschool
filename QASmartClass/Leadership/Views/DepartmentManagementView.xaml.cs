using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Leadership.Views
{
    public partial class DepartmentManagementView : Page
    {
        private readonly AppDbContext _db;
        private readonly DepartmentService _deptService;
        private List<StaffProfile> _allStaffList;

        public DepartmentManagementView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            _db = new AppDbContext();
            _deptService = new DepartmentService(_db);
            Unloaded += (s, e) => { _db?.Dispose(); };
            _allStaffList = new List<StaffProfile>();

            // WI-09: Synchronize UserSessionService session role guard
            if (QASmartClass.Staff.Services.StaffSession.CurrentUser != null)
            {
                UserSessionService.Instance.SetSession(QASmartClass.Staff.Services.StaffSession.CurrentUser.TeacherCode);
            }

            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                // Clear any selection state
                lstDepartments.SelectionChanged -= LstDepartments_SelectionChanged;

                // Load all staff for dropdowns
                _allStaffList = _db.StaffProfiles.OrderBy(s => s.FullName).ToList();

                // Bind dropdowns in details and new dialog
                cmbHeadTeacher.ItemsSource = _allStaffList;
                cmbDeputyHead.ItemsSource = _allStaffList;
                cmbNewHead.ItemsSource = _allStaffList;

                // Load departments
                var depts = _deptService.GetAllDepartments();
                lstDepartments.ItemsSource = depts;

                lstDepartments.SelectionChanged += LstDepartments_SelectionChanged;

                if (depts.Count > 0)
                {
                    lstDepartments.SelectedIndex = 0;
                }
                else
                {
                    ClearDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDetails()
        {
            txtDeptName.Text = string.Empty;
            cmbHeadTeacher.SelectedIndex = -1;
            cmbDeputyHead.SelectedIndex = -1;
            txtSchoolYear.Text = string.Empty;

            txtKpiTeachers.Text = "0";
            txtKpiLessonPlans.Text = "0";
            txtKpiObservations.Text = "0";

            dgMembers.ItemsSource = null;
            cmbAvailableStaff.ItemsSource = null;
        }

        private void LstDepartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstDepartments.SelectedItem is Department selectedDept)
            {
                RefreshSelectedDeptInfo(selectedDept);
            }
            else
            {
                ClearDetails();
            }
        }

        private void RefreshSelectedDeptInfo(Department selectedDept)
        {
            try
            {
                txtDeptName.Text = selectedDept.DepartmentName;
                txtSchoolYear.Text = selectedDept.SchoolYear;

                cmbHeadTeacher.SelectedValue = selectedDept.HeadTeacherId;
                cmbDeputyHead.SelectedValue = selectedDept.DeputyHeadId;

                // Fetch monthly KPIs (current month & year)
                var now = DateTime.Now;
                var kpis = _deptService.GetDepartmentKPI(selectedDept.Id, now.Month, now.Year);
                txtKpiTeachers.Text = kpis.TeacherCount.ToString();
                txtKpiLessonPlans.Text = kpis.LessonPlanCount.ToString();
                txtKpiObservations.Text = kpis.ObservationCount.ToString();

                // Fetch members
                LoadDepartmentMembers(selectedDept.Id);

                // Fetch available staff who aren't in any department
                LoadAvailableStaff();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin tổ chuyên môn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDepartmentMembers(int deptId)
        {
            try
            {
                var members = _db.DepartmentMembers
                    .Where(m => m.DepartmentId == deptId)
                    .ToList()
                    .Join(_db.StaffProfiles,
                          m => m.StaffId,
                          s => s.Id,
                          (m, s) => new MemberDisplayItem
                          {
                              Id = s.Id,
                              StaffCode = s.StaffCode,
                              FullName = s.FullName,
                              Role = m.Role,
                              JoinedDate = m.JoinedDate
                          })
                    .OrderBy(m => m.Role == "Tổ trưởng" ? 0 : (m.Role == "Tổ phó" ? 1 : 2))
                    .ThenBy(m => m.FullName)
                    .ToList();

                dgMembers.ItemsSource = members;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp danh sách thành viên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableStaff()
        {
            try
            {
                var memberStaffIds = _db.DepartmentMembers.Select(m => m.StaffId).ToList();
                var availableStaff = _allStaffList
                    .Where(s => !memberStaffIds.Contains(s.Id))
                    .OrderBy(s => s.FullName)
                    .ToList();

                cmbAvailableStaff.ItemsSource = availableStaff;
                if (availableStaff.Count > 0)
                {
                    cmbAvailableStaff.SelectedIndex = 0;
                }
                else
                {
                    cmbAvailableStaff.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách giáo viên tự do: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add department dialog controls
        private void BtnAddDepartment_Click(object sender, RoutedEventArgs e)
        {
            txtNewDeptName.Text = string.Empty;
            cmbNewHead.SelectedIndex = -1;
            txtNewSchoolYear.Text = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
            popAddDept.IsOpen = true;
        }

        private void BtnCancelNewDept_Click(object sender, RoutedEventArgs e)
        {
            popAddDept.IsOpen = false;
        }

        private void BtnConfirmAddDept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = txtNewDeptName.Text.Trim();
                string head = cmbNewHead.SelectedValue as string ?? string.Empty;
                string year = txtNewSchoolYear.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Vui lòng nhập tên Tổ chuyên môn mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = _deptService.CreateDepartment(name, head, year);
                if (success)
                {
                    popAddDept.IsOpen = false;
                    LoadAllData();
                    
                    // Reselect the newly created department
                    foreach (Department item in lstDepartments.Items)
                    {
                        if (item.DepartmentName == name && item.SchoolYear == year)
                        {
                            lstDepartments.SelectedItem = item;
                            break;
                        }
                    }
                    MessageBox.Show("Thêm Tổ chuyên môn mới thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Thêm Tổ chuyên môn thất bại. Tên tổ có thể đã tồn tại trong năm học này.", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveDept_Click(object sender, RoutedEventArgs e)
        {
            if (lstDepartments.SelectedItem is not Department selectedDept) return;

            try
            {
                string name = txtDeptName.Text.Trim();
                string head = cmbHeadTeacher.SelectedValue as string ?? string.Empty;
                string deputy = cmbDeputyHead.SelectedValue as string ?? string.Empty;

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Tên tổ chuyên môn không thể để trống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = _deptService.UpdateDepartment(selectedDept.Id, name, head, deputy);
                if (success)
                {
                    int selectedId = selectedDept.Id;
                    LoadAllData();

                    // Reselect
                    foreach (Department item in lstDepartments.Items)
                    {
                        if (item.Id == selectedId)
                        {
                            lstDepartments.SelectedItem = item;
                            break;
                        }
                    }
                    MessageBox.Show("Cập nhật thông tin tổ chuyên môn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Cập nhật thông tin thất bại. Vui lòng đảm bảo Tổ trưởng và Tổ phó là hai người khác nhau.", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteDept_Click(object sender, RoutedEventArgs e)
        {
            if (lstDepartments.SelectedItem is not Department selectedDept) return;

            try
            {
                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa Tổ chuyên môn '{selectedDept.DepartmentName}'?", 
                    "Xác nhận xóa tổ chuyên môn", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (confirm == MessageBoxResult.Yes)
                {
                    bool success = _deptService.DeleteDepartment(selectedDept.Id);
                    if (success)
                    {
                        LoadAllData();
                        MessageBox.Show("Đã xóa tổ chuyên môn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa tổ chuyên môn này do vẫn còn chứa thành viên. Vui lòng xóa hết thành viên trước khi xóa tổ.", 
                            "Không cho phép xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            if (lstDepartments.SelectedItem is not Department selectedDept) return;

            try
            {
                if (cmbAvailableStaff.SelectedValue is int staffId)
                {
                    string role = (cmbMemberRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Thành viên";
                    bool success = _deptService.AddMember(selectedDept.Id, staffId, role);
                    if (success)
                    {
                        RefreshSelectedDeptInfo(selectedDept);
                        MessageBox.Show("Đã thêm thành viên vào tổ chuyên môn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Thêm thành viên thất bại.", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một giáo viên từ danh sách để thêm vào tổ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (lstDepartments.SelectedItem is not Department selectedDept) return;

            try
            {
                if (sender is Button btn && btn.DataContext is MemberDisplayItem member)
                {
                    var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa thành viên '{member.FullName}' khỏi tổ chuyên môn '{selectedDept.DepartmentName}'?", 
                        "Xác nhận xóa thành viên", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (confirm == MessageBoxResult.Yes)
                    {
                        bool success = _deptService.RemoveMember(selectedDept.Id, member.Id);
                        if (success)
                        {
                            RefreshSelectedDeptInfo(selectedDept);
                            MessageBox.Show("Đã xóa thành viên khỏi tổ thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Xóa thành viên thất bại.", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MemberDisplayItem
    {
        public int Id { get; set; }
        public string StaffCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
    }
}

