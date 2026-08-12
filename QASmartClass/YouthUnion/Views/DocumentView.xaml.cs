using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using QASmartClass.YouthUnion.Services;
using System.Threading.Tasks;

namespace QASmartClass.YouthUnion.Views
{
    public partial class DocumentView : Page
    {
        private readonly YouthUnionService _service;

        public DocumentView()
        {
            InitializeComponent();
            _service = new YouthUnionService();
Unloaded += (s, e) => { _service?.Dispose(); };
            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_service == null) return;
            try
            {
                var catFilter = (CbCategory?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
                var allDocs = await _service.GetAllAsync<YouthDocument>();
                var query = allDocs.AsQueryable();
                if (catFilter != "All") query = query.Where(d => d.Category == catFilter);
                DgDocs.ItemsSource = query.OrderByDescending(d => d.UploadedAt).ToList();
            }
            catch (Exception ex) { Log.Error(ex, "Document load error"); MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void CbCategory_Changed(object sender, SelectionChangedEventArgs e) => await LoadDataAsync();

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "All Files|*.*|PDF Documents|*.pdf|Word Documents|*.docx;*.doc" };
            if (dlg.ShowDialog() == true)
            {
                var w = new Window { Title = "Chi tiết tài liệu", Width = 400, Height = 250, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };
                sp.Children.Add(new TextBlock { Text = "Tên tài liệu:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtTitle = new TextBox { Text = Path.GetFileNameWithoutExtension(dlg.FileName), Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtTitle);

                sp.Children.Add(new TextBlock { Text = "Danh mục:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbCat = new ComboBox { Height = 32 };
                cbCat.Items.Add("Quy chế"); cbCat.Items.Add("Hướng dẫn"); cbCat.Items.Add("Biểu mẫu"); cbCat.Items.Add("Khác");
                cbCat.SelectedIndex = 3;
                sp.Children.Add(cbCat);

                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtTitle.Text)) return;
                    try
                    {
                        var destDir = Path.Combine(AppPaths.RootDir, "YouthDocs");
                        Directory.CreateDirectory(destDir);
                        var destFile = Path.Combine(destDir, Guid.NewGuid().ToString() + Path.GetExtension(dlg.FileName));
                        await Task.Run(() => File.Copy(dlg.FileName, destFile, true));

                        var selCat = cbCat.SelectedItem?.ToString();
                        await _service.AddAsync(new YouthDocument
                        {
                            Title = txtTitle.Text.Trim(),
                            Category = selCat == "Quy chế" ? "Quy che" : selCat == "Hướng dẫn" ? "Huong dan" : selCat == "Biểu mẫu" ? "Bieu mau" : "General",
                            FilePath = destFile,
                            UploadedBy = Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "System",
                            UploadedAt = DateTime.Now
                        });
                        w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Upload error"); MessageBox.Show("Có lỗi xảy ra khi tải tài liệu lên hệ thống. Vui lòng thử lại sau.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filePath && File.Exists(filePath))
            {
                var dlg = new SaveFileDialog { FileName = Path.GetFileName(filePath) };
                if (dlg.ShowDialog() == true)
                {
                    try 
                    { 
                        btn.IsEnabled = false;
                        await Task.Run(() => File.Copy(filePath, dlg.FileName, true)); 
                        MessageBox.Show("Tải tài liệu xuống thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information); 
                    }
                    catch (Exception ex) { MessageBox.Show("Không thể tải tài liệu xuống. Vui lòng kiểm tra lại kết nối mạng.\n\nChi tiết: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error); }
                    finally { btn.IsEnabled = true; }
                }
            }
        }

        private async void BtnEditDoc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var allDocs = await _service.GetAllAsync<YouthDocument>();
                var doc = allDocs.FirstOrDefault(d => d.Id == id);
                if (doc == null) return;
                var w = new Window { Title = "Sửa tài liệu", Width = 400, Height = 250, WindowStartupLocation = WindowStartupLocation.CenterScreen, FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Tên tài liệu:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 5) });
                var txtTitle = new TextBox { Text = doc.Title, Height = 32, Padding = new Thickness(8, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
                sp.Children.Add(txtTitle);

                sp.Children.Add(new TextBlock { Text = "Danh mục:", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 5) });
                var cbCat = new ComboBox { Height = 32 };
                cbCat.Items.Add("Quy chế"); cbCat.Items.Add("Hướng dẫn"); cbCat.Items.Add("Biểu mẫu"); cbCat.Items.Add("Khác");
                cbCat.SelectedItem = doc.Category == "Quy che" ? "Quy chế" : doc.Category == "Huong dan" ? "Hướng dẫn" : doc.Category == "Bieu mau" ? "Biểu mẫu" : "Khác";
                sp.Children.Add(cbCat);

                var btnSave = new Button { Content = "Lưu", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(15, 10, 15, 10), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)), Foreground = System.Windows.Media.Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
                sp.Children.Add(btnSave);

                btnSave.Click += async (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Vui lòng nhập tên tài liệu trước khi tiếp tục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    try
                    {
                        doc.Title = txtTitle.Text.Trim();
                        var selCat = cbCat.SelectedItem?.ToString();
                        doc.Category = selCat == "Quy chế" ? "Quy che" : selCat == "Hướng dẫn" ? "Huong dan" : selCat == "Biểu mẫu" ? "Bieu mau" : "General";
                        await _service.UpdateAsync(doc); w.Close(); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Edit doc error"); MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                };
                w.Content = sp; w.Owner = Window.GetWindow(this); w.ShowDialog();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Xác nhận xóa?", "Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _service.DeleteAsync<YouthDocument>(id); await LoadDataAsync();
                    }
                    catch (Exception ex) { Log.Error(ex, "Delete doc error"); MessageBox.Show($"Lỗi xóa tài liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            try
            {
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("QA SmartClass");
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "KhoTaiLieu.xlsx" };
                if (dlg.ShowDialog() == true)
                {
                    if (btn != null) btn.IsEnabled = false;
                    using var package = new OfficeOpenXml.ExcelPackage();
                    QASmartClass.YouthUnion.Services.YouthExportHelper.EncryptWorkbook(package);
                    var ws = package.Workbook.Worksheets.Add("TaiLieu");
                    ws.Cells["A1"].Value = "STT";
                    ws.Cells["B1"].Value = "Tên tài liệu";
                    ws.Cells["C1"].Value = "Danh mục";
                    ws.Cells["D1"].Value = "Người tải lên";
                    ws.Cells["E1"].Value = "Ngày tải lên";
                    ws.Cells["A1:E1"].Style.Font.Bold = true;

                    var items = DgDocs.ItemsSource as System.Collections.IEnumerable;
                    int row = 2;
                    if (items != null)
                    {
                        foreach (dynamic doc in items)
                        {
                            ws.Cells[row, 1].Value = row - 1;
                            ws.Cells[row, 2].Value = doc.Title;
                            ws.Cells[row, 3].Value = doc.Category;
                            ws.Cells[row, 4].Value = doc.UploadedBy;
                            ws.Cells[row, 5].Value = doc.UploadedAt.ToString("dd/MM/yyyy");
                            row++;
                        }
                    }
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    var bytes = package.GetAsByteArray();
                    await Task.Run(() => System.IO.File.WriteAllBytes(dlg.FileName, bytes));
                    MessageBox.Show("Xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export Excel error");
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }

    public class DocumentCategoryConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string cat)
            {
                if (cat == "Quy che") return "Quy chế";
                if (cat == "Huong dan") return "Hướng dẫn";
                if (cat == "Bieu mau") return "Biểu mẫu";
                if (cat == "General") return "Khác";
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}

