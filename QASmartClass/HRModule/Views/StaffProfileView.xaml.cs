using QASmartClass.Data;
using System.Globalization;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.HRModule.Views
{
    public partial class StaffProfileView : Page
    {
        private readonly AppDbContext _db;

        public StaffProfileView()
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
                if (!_db.StaffProfiles.Any())
                {
                    _db.StaffProfiles.AddRange(
                        new StaffProfile { StaffCode = "GV001", FullName = "Nguyễn Văn A", Department = "Tổ Toán", Position = "Giáo viên", Email = "nva@school.edu.vn", Phone = "0901234567", BaseSalary = 10000000, JoinedDate = new DateTime(2020, 8, 1) },
                        new StaffProfile { StaffCode = "NV001", FullName = "Trần Thị B", Department = "Văn phòng", Position = "Kế toán", Email = "ttb@school.edu.vn", Phone = "0909876543", BaseSalary = 8000000, JoinedDate = new DateTime(2021, 5, 1) }
                    );
                    _db.SaveChanges();
                }

                DgStaff.ItemsSource = _db.StaffProfiles.ToList();
            }
            catch (Exception ex) { Log.Warning("StaffProfile Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var w = new Window
            {
                Title = "Thêm Nhân Sự Mới",
                Width = 400,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252))
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Form
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Header
            var header = new TextBlock { Text = "👤 Thông tin nhân sự mới", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42)), Margin = new Thickness(0, 0, 0, 15) };
            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            // Form
            var sp = new StackPanel();
            
            sp.Children.Add(new TextBlock { Text = "Mã nhân sự:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtCode = new TextBox { Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(txtCode);

            sp.Children.Add(new TextBlock { Text = "Họ và tên:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtName = new TextBox { Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(txtName);

            sp.Children.Add(new TextBlock { Text = "Phòng ban:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var cbDept = new ComboBox { Height = 32, Margin = new Thickness(0, 0, 0, 10) };
            cbDept.Items.Add("Tổ Toán"); cbDept.Items.Add("Tổ Văn"); cbDept.Items.Add("Tổ Anh"); cbDept.Items.Add("Văn phòng"); cbDept.Items.Add("Ban Giám Hiệu"); cbDept.SelectedIndex = 0;
            sp.Children.Add(cbDept);

            sp.Children.Add(new TextBlock { Text = "Chức vụ:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtPos = new TextBox { Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            txtPos.Text = "Giáo viên";
            sp.Children.Add(txtPos);

            sp.Children.Add(new TextBlock { Text = "Email:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtEmail = new TextBox { Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(txtEmail);

            sp.Children.Add(new TextBlock { Text = "Số điện thoại:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtPhone = new TextBox { Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(txtPhone);

            sp.Children.Add(new TextBlock { Text = "Lương cơ bản (VNĐ):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            var txtSalary = new TextBox { Text = "8000000", Height = 32, Padding = new Thickness(6, 0, 6, 0), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 15) };
            sp.Children.Add(txtSalary);

            var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            grid.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            // Buttons
            var btnGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(0, 15, 0, 0) };
            var btnCancel = new Button { Content = "Hủy bỏ", Height = 36, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)), Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 109)), FontWeight = FontWeights.SemiBold, Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 8, 0) };
            var btnSave = new Button { Content = "Lưu hồ sơ", Height = 36, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(0), Margin = new Thickness(8, 0, 0, 0) };
            
            btnCancel.Click += (s, ev) => w.Close();
            btnSave.Click += (s, ev) =>
            {
                string code = txtCode.Text.Trim();
                string name = txtName.Text.Trim();
                string dept = cbDept.SelectedItem?.ToString() ?? "Tổ Toán";
                string pos = txtPos.Text.Trim();
                string email = txtEmail.Text.Trim();
                string phone = txtPhone.Text.Trim();
                
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Mã nhân sự và Họ tên không được để trống.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(txtSalary.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double salary) || salary < 0)
                {
                    MessageBox.Show("Lương cơ bản không hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{10}$"))
                {
                    MessageBox.Show("Số điện thoại phải chứa đúng 10 chữ số.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    MessageBox.Show("Email không đúng định dạng tiêu chuẩn.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var exists = _db.StaffProfiles.Any(x => x.StaffCode == code);
                    if (exists)
                    {
                        MessageBox.Show("Mã nhân sự này đã tồn tại trong hệ thống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var profile = new StaffProfile
                    {
                        StaffCode = code,
                        FullName = name,
                        Department = dept,
                        Position = pos,
                        Email = email,
                        Phone = phone,
                        BaseSalary = salary,
                        JoinedDate = DateTime.Today
                    };

                    _db.StaffProfiles.Add(profile);
                    _db.SaveChanges();
                    w.Close();
                    LoadData();
                    MessageBox.Show("Thêm nhân sự mới thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu cơ sở dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnGrid.Children.Add(btnCancel);
            btnGrid.Children.Add(btnSave);
            grid.Children.Add(btnGrid);
            Grid.SetRow(btnGrid, 2);

            w.Content = grid;
            w.ShowDialog();
        }
    }
}

