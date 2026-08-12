using OfficeOpenXml;
using Microsoft.Win32;
using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Threading.Tasks;
using QASmartClass.YouthUnion.Services;

namespace QASmartClass.YouthUnion.Views
{
    public partial class EmulationScoreView : Page
    {
        private readonly YouthUnionService _service;

        public EmulationScoreView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_service == null) return;
            try
            {
                var allMembers = await _service.GetAllAsync<YouthMember>();
                var members = allMembers.Where(m => m.Status == "Active")
                    .OrderByDescending(m => m.TotalScore).ToList();
                int rank = 1;
                var display = members.Select(m => new
                {
                    Id = m.Id,
                    Rank = rank++,
                    m.StudentName,
                    m.ClassName,
                    m.Position,
                    m.TotalScore,
                    Grade = m.TotalScore >= 80 ? "Xuất sắc" : m.TotalScore >= 60 ? "Tốt" : m.TotalScore >= 40 ? "Khá" : "TB"
                }).ToList();
                
                string search = TxtSearch?.Text?.Trim().ToLower() ?? "";
                if (!string.IsNullOrEmpty(search))
                {
                    display = display.Where(x => 
                        (x.StudentName != null && x.StudentName.ToLower().Contains(search)) ||
                        (x.ClassName != null && x.ClassName.ToLower().Contains(search))
                    ).ToList();
                }

                DgScores.ItemsSource = display;
            }
            catch (Exception ex) 
            { 
                Log.Error(ex, "EmulationScore load error"); 
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void BtnAddScore_Click(object sender, RoutedEventArgs e) => await ShowScoreFormAsync(true);
        private async void BtnSubScore_Click(object sender, RoutedEventArgs e) => await ShowScoreFormAsync(false);

        private async Task ShowScoreFormAsync(bool isAdd)
        {
            var w = new Window
            {
                Title = isAdd ? "Cộng điểm thi đua" : "Trừ điểm thi đua",
                Width = 420, Height = 380, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Chọn Đoàn viên:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var cbMember = new ComboBox { Height = 32, DisplayMemberPath = "DisplayName" };
            var allMembers = await _service.GetAllAsync<YouthMember>();
            var members = allMembers.Where(m => m.Status == "Active").ToList()
                .Select(m => new { m.Id, DisplayName = $"{m.StudentName} ({m.ClassName})" }).ToList();
            cbMember.ItemsSource = members;
            if (members.Any()) cbMember.SelectedIndex = 0;
            sp.Children.Add(cbMember);

            sp.Children.Add(new TextBlock { Text = "Số điểm:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtPts = new TextBox { Text = "10", Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center, FontSize = 14 };
            sp.Children.Add(txtPts);

            sp.Children.Add(new TextBlock { Text = "Lý do:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtReason = new TextBox { Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8), FontSize = 14 };
            sp.Children.Add(txtReason);

            var color = isAdd ? System.Windows.Media.Color.FromRgb(16, 185, 129) : System.Windows.Media.Color.FromRgb(239, 68, 68);
            var btnSave = new Button
            {
                Content = isAdd ? "Cộng điểm" : "Trừ điểm",
                Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10),
                Background = new System.Windows.Media.SolidColorBrush(color),
                Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) =>
            {
                if (!int.TryParse(txtPts.Text, out int pts) || pts <= 0 || pts > 50) { MessageBox.Show("Vui lòng nhập số điểm hợp lệ từ 1 đến 50 cho mỗi lần ghi nhận thi đua.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                dynamic? sel = cbMember.SelectedItem;
                if (sel == null) { MessageBox.Show("Vui lòng chọn Đoàn viên.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                int memberId = sel.Id;
                try
                {
                    int actualScore = isAdd ? pts : -pts;
                    await _service.AddAsync(new YouthEmulationScore
                    {
                        MemberId = memberId,
                        Score = actualScore,
                        Reason = txtReason.Text.Trim(),
                        AwardedDate = DateTime.Today,
                        AwardedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                        Category = isAdd ? "Activity" : "Discipline"
                    });
                    var allMems = await _service.GetAllAsync<YouthMember>();
                    var member = allMems.FirstOrDefault(m => m.Id == memberId);
                    if (member != null)
                    {
                        member.TotalScore += actualScore;
                        if (member.TotalScore < 0) member.TotalScore = 0;
                        await _service.UpdateAsync(member);
                    }
                    w.Close();
                    await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Score update error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp;
            w.ShowDialog();
        }

        private async void BtnHistoryScore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int memberId)
            {
                await ShowScoreHistoryAsync(memberId);
            }
        }

        private async Task ShowScoreHistoryAsync(int memberId)
        {
            var allMembers = await _service.GetAllAsync<YouthMember>();
            var member = allMembers.FirstOrDefault(m => m.Id == memberId);
            if (member == null) return;

            var w = new Window
            {
                Title = $"Lịch sử thi đua - {member.StudentName} ({member.ClassName})",
                Width = 620, Height = 450, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var title = new TextBlock { Text = $"Tổng điểm tích lũy: {member.TotalScore}", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            header.Children.Add(title);
            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            var dg = new DataGrid
            {
                AutoGenerateColumns = false, IsReadOnly = true, BorderThickness = new Thickness(1),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
                HeadersVisibility = DataGridHeadersVisibility.Column, RowHeight = 36, FontSize = 13,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, RowBackground = System.Windows.Media.Brushes.White,
                AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252))
            };

            dg.Columns.Add(new DataGridTextColumn { Header = "Ngày", Binding = new System.Windows.Data.Binding("AwardedDate") { StringFormat = "dd/MM/yyyy" }, Width = new DataGridLength(100) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Điểm", Binding = new System.Windows.Data.Binding("Score"), Width = new DataGridLength(60) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Lý do", Binding = new System.Windows.Data.Binding("Reason"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Người thực hiện", Binding = new System.Windows.Data.Binding("AwardedBy"), Width = new DataGridLength(110) });

            // Action column
            var templateCol = new DataGridTemplateColumn { Header = "Thao tác", Width = new DataGridLength(100) };
            var dt = new DataTemplate();
            var spFactory = new FrameworkElementFactory(typeof(StackPanel));
            spFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var btnEditFactory = new FrameworkElementFactory(typeof(Button));
            btnEditFactory.SetValue(Button.ContentProperty, "Sửa");
            btnEditFactory.SetValue(Button.ToolTipProperty, "Sửa");
            btnEditFactory.SetValue(Button.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            btnEditFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            btnEditFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);
            btnEditFactory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("Id"));
            btnEditFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(async (s, ev) =>
            {
                if (s is Button btn && btn.Tag is int scoreId)
                {
                    await EditIndividualScoreAsync(scoreId, w, dg, member);
                }
            }));
            spFactory.AppendChild(btnEditFactory);

            var btnDelFactory = new FrameworkElementFactory(typeof(Button));
            btnDelFactory.SetValue(Button.ContentProperty, "Xóa");
            btnDelFactory.SetValue(Button.ToolTipProperty, "Xóa");
            btnDelFactory.SetValue(Button.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            btnDelFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            btnDelFactory.SetValue(Button.CursorProperty, System.Windows.Input.Cursors.Hand);
            btnDelFactory.SetValue(Button.MarginProperty, new Thickness(8, 0, 0, 0));
            btnDelFactory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding("Id"));
            btnDelFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(async (s, ev) =>
            {
                if (s is Button btn && btn.Tag is int scoreId)
                {
                    await DeleteIndividualScoreAsync(scoreId, w, dg, member);
                }
            }));
            spFactory.AppendChild(btnDelFactory);

            dt.VisualTree = spFactory;
            templateCol.CellTemplate = dt;
            dg.Columns.Add(templateCol);

            // Load data for this member
            var loadHistory = new Func<Task>(async () =>
            {
                var allScores = await _service.GetAllAsync<YouthEmulationScore>();
                dg.ItemsSource = allScores.Where(x => x.MemberId == member.Id).OrderByDescending(x => x.AwardedDate).ToList();
                title.Text = $"Tổng điểm tích lũy: {member.TotalScore}";
            });
            await loadHistory();

            grid.Children.Add(dg);
            Grid.SetRow(dg, 1);

            w.Content = grid;
            w.ShowDialog();
        }

        private async Task EditIndividualScoreAsync(int scoreId, Window historyWindow, DataGrid historyGrid, YouthMember member)
        {
            var allScores = await _service.GetAllAsync<YouthEmulationScore>();
            var item = allScores.FirstOrDefault(s => s.Id == scoreId);
            if (item == null) return;

            var w = new Window { Title = "Sửa điểm thi đua", Width = 350, Height = 280, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Điểm:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtScore = new TextBox { Text = Math.Abs(item.Score).ToString(), Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
            sp.Children.Add(txtScore);

            sp.Children.Add(new TextBlock { Text = "Lý do:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
            var txtReason = new TextBox { Text = item.Reason, Height = 50, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(8) };
            sp.Children.Add(txtReason);

            var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
            sp.Children.Add(btnSave);

            btnSave.Click += async (s, ev) => {
                if (!int.TryParse(txtScore.Text, out int newScore) || newScore <= 0) { MessageBox.Show("Nhập điểm hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                string reason = txtReason.Text.Trim();
                if (!IsValidReason(reason)) 
                { 
                    MessageBox.Show("Lý do thay đổi phải có ý nghĩa (tối thiểu 10 ký tự, không nhập ký tự hoặc số lặp lại vô nghĩa).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); 
                    return; 
                }
                try {
                    int oldScore = item.Score;
                    int actualNew = item.Score < 0 ? -newScore : newScore;
                    item.Score = actualNew;
                    item.Reason = reason;
                    
                    // Recalculate TotalScore
                    member.TotalScore += (actualNew - oldScore);
                    if (member.TotalScore < 0) member.TotalScore = 0;
                    
                    await _service.UpdateAsync(item);
                    await _service.UpdateAsync(member);
 
                    // Ghi log kiểm toán
                    await _service.AddAsync(new AuditLog
                    {
                        Action = "EDIT_EMULATION_SCORE",
                        ActorName = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                        Details = $"Sửa điểm thi đua ID {item.Id} của Đoàn viên {member.StudentName}. Lý do: {reason}",
                        Timestamp = DateTime.Now
                    });
 
                    w.Close();
                    
                    // Reload history grid and update labels
                    var refreshedScores = await _service.GetAllAsync<YouthEmulationScore>();
                    historyGrid.ItemsSource = refreshedScores.Where(x => x.MemberId == member.Id).OrderByDescending(x => x.AwardedDate).ToList();
                    var titleTextBlock = ((DockPanel)((Grid)historyWindow.Content).Children[0]).Children[0] as TextBlock;
                    if (titleTextBlock != null) titleTextBlock.Text = $"Tổng điểm tích lũy: {member.TotalScore}";
                    
                    await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Edit score error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            w.Content = sp; w.ShowDialog();
        }
 
        private async Task DeleteIndividualScoreAsync(int scoreId, Window historyWindow, DataGrid historyGrid, YouthMember member)
        {
            var allScores = await _service.GetAllAsync<YouthEmulationScore>();
            var item = allScores.FirstOrDefault(s => s.Id == scoreId);
            if (item == null) return;
 
            var prompt = new Window 
            { 
                Title = "Nhập lý do xóa điểm thi đua", 
                Width = 360, Height = 180, 
                Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI") 
            };
            var panel = new StackPanel { Margin = new Thickness(15) };
            panel.Children.Add(new TextBlock { Text = "Lý do xóa điểm thi đua (bắt buộc):", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
            var txtDelReason = new TextBox { Height = 40, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Padding = new Thickness(5) };
            panel.Children.Add(txtDelReason);
            
            var btnConfirm = new Button 
            { 
                Content = "Xác nhận xóa", 
                Margin = new Thickness(0, 15, 0, 0), Padding = new Thickness(10), 
                Background = System.Windows.Media.Brushes.Red, Foreground = System.Windows.Media.Brushes.White, 
                FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand 
            };
            panel.Children.Add(btnConfirm);
            prompt.Content = panel;
 
            btnConfirm.Click += async (s, ev) => 
            {
                string delReason = txtDelReason.Text.Trim();
                if (!IsValidReason(delReason))
                {
                    MessageBox.Show("Lý do xóa phải có ý nghĩa (tối thiểu 10 ký tự, không nhập ký tự hoặc số lặp lại vô nghĩa).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                try {
                    member.TotalScore -= item.Score;
                    if (member.TotalScore < 0) member.TotalScore = 0;
                    
                    await _service.DeleteAsync<YouthEmulationScore>(item.Id);
                    await _service.UpdateAsync(member);
                    
                    // Ghi log kiểm toán
                    await _service.AddAsync(new AuditLog
                    {
                        Action = "DELETE_EMULATION_SCORE",
                        ActorName = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                        Details = $"Xóa điểm thi đua ID {item.Id} ({item.Score} điểm) của Đoàn viên {member.StudentName}. Lý do: {delReason}",
                        Timestamp = DateTime.Now
                    });
                    
                    prompt.Close();
                    
                    // Reload history grid
                    var refreshedScores = await _service.GetAllAsync<YouthEmulationScore>();
                    historyGrid.ItemsSource = refreshedScores.Where(x => x.MemberId == member.Id).OrderByDescending(x => x.AwardedDate).ToList();
                    var titleTextBlock = ((DockPanel)((Grid)historyWindow.Content).Children[0]).Children[0] as TextBlock;
                    if (titleTextBlock != null) titleTextBlock.Text = $"Tổng điểm tích lũy: {member.TotalScore}";
                    
                    await LoadDataAsync();
                }
                catch (Exception ex) { Log.Error(ex, "Delete score error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            prompt.ShowDialog();
        }
 
        private bool IsValidReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return false;
            reason = reason.Trim();
            if (reason.Length < 10) return false;
 
            // Không được toàn ký tự đặc biệt lặp lại (vd: ..... hoặc -----)
            if (reason.Distinct().Count() == 1) return false;
 
            // Không được chứa chuỗi số tuần tự hoặc ký tự lặp vô nghĩa (vd: 1234567890, aaaaaaaaaa)
            bool isAllDigits = reason.All(char.IsDigit);
            if (isAllDigits) return false;

 
            var cleanText = reason.Replace(" ", "").Replace(".", "").Replace("-", "");
            if (cleanText.Length < 5) return false;
 
            return true;
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "BangThiDuaDoanVien.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    using var package = new ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("ThiDua");
                    ws.Cells["A1"].Value = "Hạng";
                    ws.Cells["B1"].Value = "Họ và Tên";
                    ws.Cells["C1"].Value = "Lớp";
                    ws.Cells["D1"].Value = "Chức vụ";
                    ws.Cells["E1"].Value = "Tổng điểm";
                    ws.Cells["F1"].Value = "Xếp loại";
                    ws.Cells["A1:F1"].Style.Font.Bold = true;

                    var items = DgScores.ItemsSource as System.Collections.IEnumerable;
                    int row = 2;
                    if (items != null)
                    {
                        foreach (dynamic s in items)
                        {
                            ws.Cells[row, 1].Value = s.Rank;
                            ws.Cells[row, 2].Value = s.StudentName;
                            ws.Cells[row, 3].Value = s.ClassName;
                            ws.Cells[row, 4].Value = s.Position;
                            ws.Cells[row, 5].Value = s.TotalScore;
                            ws.Cells[row, 6].Value = s.Grade;
                            row++;
                        }
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    
                    byte[] fileBytes = package.GetAsByteArray();
                    await Task.Run(() => File.WriteAllBytes(dlg.FileName, fileBytes));
                    
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { Log.Error(ex, "Export Excel error"); MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally
            {
                if (sender is Button btnLock) btnLock.IsEnabled = true;
            }
        }
    }
}
