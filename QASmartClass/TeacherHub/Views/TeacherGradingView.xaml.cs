using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.TeacherHub.Views
{
    public partial class TeacherGradingView : UserControl
    {
        private readonly System.Collections.Generic.Stack<System.Data.DataTable> _undoStack = new();
        private readonly System.Collections.Generic.Stack<System.Data.DataTable> _redoStack = new();

        public TeacherGradingView()
        {
            InitializeComponent();
            this.PreviewKeyDown += TeacherGradingView_PreviewKeyDown;
            this.Loaded += (s, e) => RegisterDataGridEvents();
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> ColumnHeaderMap = new()
        {
            { "StudentName", "Họ và tên" },
            { "Score", "Điểm" },
            { "Subject", "Môn học" },
            { "Grade", "Lớp" },
            { "ExamType", "Loại KT" },
            { "Semester", "Học kỳ" },
            { "CreatedAt", "Ngày tạo" },
            { "UpdatedAt", "Cập nhật" },
            { "Note", "Ghi chú" },
            { "Status", "Trạng thái" },
            { "ClassName", "Tên lớp" },
            { "FullName", "Họ tên" }
        };

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Auto-translate header
            if (ColumnHeaderMap.ContainsKey(e.PropertyName))
                e.Column.Header = ColumnHeaderMap[e.PropertyName];

            if (e.PropertyName == "StudentId" || e.PropertyName == "Mã HS" || e.PropertyName == "Id")
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
            else if (e.PropertyName == "STT")
            {
                e.Column.IsReadOnly = true;
                e.Column.Width = new DataGridLength(60);
            }
            else if (e.PropertyName == "Họ và tên")
            {
                e.Column.IsReadOnly = true;
                e.Column.Width = new DataGridLength(250);
                
                // Align left for name
                var style = new Style(typeof(DataGridCell));
                style.Setters.Add(new Setter(PaddingProperty, new Thickness(10, 0, 0, 0)));
                style.Setters.Add(new Setter(TemplateProperty, CreateCellTemplate(HorizontalAlignment.Left)));
                e.Column.CellStyle = style;
            }
            else
            {
                // Grade columns
                e.Column.Width = new DataGridLength(120);
            }
        }

        private ControlTemplate CreateCellTemplate(HorizontalAlignment alignment)
        {
            var template = new ControlTemplate(typeof(DataGridCell));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(DataGridCell.PaddingProperty));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(DataGridCell.BackgroundProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, alignment);

            border.AppendChild(presenter);
            template.VisualTree = border;
            return template;
        }
        // ═══ BATCH GRADING: Paste từ Excel ═══

        public void BtnPasteExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsText()) 
                {
                    MessageBox.Show("📋 Clipboard trống. Hãy copy dữ liệu từ Excel trước.", "Paste điểm");
                    return;
                }

                var text = Clipboard.GetText();
                var lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                var parsed = new System.Collections.Generic.List<object>();
                int successCount = 0;

                foreach (var line in lines)
                {
                    var cols = line.Split('\t');
                    if (cols.Length >= 2)
                    {
                        string scoreStr = cols[1].Trim().Replace(',', '.'); // Chuẩn hóa dấu phẩy sang dấu chấm
                        if (double.TryParse(scoreStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double score))
                        {
                            if (score < 0 || score > 10) continue; // Validate 0-10
                            parsed.Add(new { Identifier = cols[0].Trim(), Score = score });
                            successCount++;
                        }
                    }
                }

                if (successCount == 0)
                {
                    MessageBox.Show("⚠ Không tìm thấy dữ liệu hợp lệ.\nĐịnh dạng: Tên_HS hoặc Mã_HS<TAB>Điểm", "Lỗi paste");
                    return;
                }

                // Map parsed data → DataGrid ItemsSource (GradingTable DataTable)
                var vm = DataContext as QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel;
                if (vm?.GradingTable != null)
                {
                    SaveState();
                    var table = vm.GradingTable;
                    string targetColName = "";
                    var dg = this.FindName("dgGrades") as DataGrid;
                    if (dg != null)
                    {
                        var currentCol = dg.CurrentColumn;
                        if (currentCol != null && table.Columns.Contains(currentCol.Header.ToString()))
                        {
                            targetColName = currentCol.Header.ToString();
                        }
                        else
                        {
                            foreach (System.Data.DataColumn col in table.Columns)
                            {
                                if (col.ColumnName != "StudentId" && col.ColumnName != "STT" && col.ColumnName != "Mã HS" && col.ColumnName != "Họ và tên")
                                {
                                    targetColName = col.ColumnName;
                                    break;
                                }
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(targetColName))
                    {
                        MessageBox.Show("Vui lòng chọn cột điểm để dán dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int matchedCount = 0;
                    var duplicateWarnings = new System.Collections.Generic.List<string>();

                    // Đọc cấu hình từ CSDL
                    string matchOption = "SkipAndWarn";
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        matchOption = db.SystemSettings.FirstOrDefault(s => s.Id == "GradingMatchDuplicateOption")?.Value ?? "SkipAndWarn";
                    }

                    foreach (var item in parsed)
                    {
                        var idProp = item.GetType().GetProperty("Identifier");
                        var scoreProp = item.GetType().GetProperty("Score");
                        if (idProp == null || scoreProp == null) continue;

                        string pIdentifier = idProp.GetValue(item)?.ToString() ?? "";
                        double pScore = (double)(scoreProp.GetValue(item) ?? 0.0);

                        string cleanPIdentifier = RemoveDiacritics(pIdentifier).Replace(" ", "").ToLower();

                        // 1. Ưu tiên so khớp chính xác tuyệt đối theo Mã học sinh
                        System.Data.DataRow codeMatch = null;
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            string dbCode = "";
                            if (table.Columns.Contains("Mã HS")) dbCode = row["Mã HS"]?.ToString() ?? "";
                            else if (table.Columns.Contains("StudentCode")) dbCode = row["StudentCode"]?.ToString() ?? "";
                            else if (table.Columns.Contains("StudentId")) dbCode = row["StudentId"]?.ToString() ?? "";

                            string cleanDbCode = dbCode.Trim().ToLower();
                            if (!string.IsNullOrEmpty(cleanDbCode) && cleanDbCode == cleanPIdentifier)
                            {
                                codeMatch = row;
                                break;
                            }
                        }

                        if (codeMatch != null)
                        {
                            codeMatch[targetColName] = pScore.ToString("0.##");
                            matchedCount++;
                            continue;
                        }

                        // 2. Dự phòng: So khớp theo Họ và tên
                        var nameMatches = new System.Collections.Generic.List<System.Data.DataRow>();
                        foreach (System.Data.DataRow row in table.Rows)
                        {
                            string dbName = row["Họ và tên"]?.ToString() ?? "";
                            if (string.IsNullOrEmpty(dbName) && table.Columns.Contains("StudentName"))
                                dbName = row["StudentName"]?.ToString() ?? "";
                            if (string.IsNullOrEmpty(dbName) && table.Columns.Contains("FullName"))
                                dbName = row["FullName"]?.ToString() ?? "";

                            string cleanDbName = RemoveDiacritics(dbName).Replace(" ", "").ToLower();
                            if (cleanDbName == cleanPIdentifier)
                            {
                                nameMatches.Add(row);
                            }
                        }

                        if (nameMatches.Count == 1)
                        {
                            nameMatches[0][targetColName] = pScore.ToString("0.##");
                            matchedCount++;
                        }
                        else if (nameMatches.Count > 1)
                        {
                            if (matchOption == "ShowSelectorDialog")
                            {
                                var selectList = new System.Collections.Generic.List<dynamic>();
                                foreach (var row in nameMatches)
                                {
                                    string sName = row["Họ và tên"]?.ToString() ?? "";
                                    string sCode = "";
                                    if (table.Columns.Contains("Mã HS")) sCode = row["Mã HS"]?.ToString() ?? "";
                                    else if (table.Columns.Contains("StudentCode")) sCode = row["StudentCode"]?.ToString() ?? "";
                                    else if (table.Columns.Contains("StudentId")) sCode = row["StudentId"]?.ToString() ?? "";

                                    int rowIndex = table.Rows.IndexOf(row);
                                    selectList.Add(new { Id = rowIndex, DisplayText = $"{sName} (MSHS: {sCode}) - Hàng {rowIndex + 1}" });
                                }

                                var win = new DuplicateStudentSelectorWindow(pIdentifier, selectList);
                                win.Owner = Window.GetWindow(this);
                                win.ShowDialog();

                                if (!win.IsCancelled && win.SelectedRowIndex >= 0)
                                {
                                    var selectedRow = table.Rows[win.SelectedRowIndex];
                                    selectedRow[targetColName] = pScore.ToString("0.##");
                                    matchedCount++;
                                }
                                else
                                {
                                    duplicateWarnings.Add($"{pIdentifier} (Bỏ qua dòng này)");
                                }
                            }
                            else
                            {
                                duplicateWarnings.Add($"{pIdentifier} (Phát hiện {nameMatches.Count} học sinh trùng tên)");
                            }
                        }
                    }

                    table.AcceptChanges();
                    dg?.Items.Refresh();

                    string resultMsg = $"✅ Đã ánh xạ và điền điểm cho {matchedCount}/{parsed.Count} học sinh vào cột '{targetColName}'.\n";
                    if (duplicateWarnings.Count > 0)
                    {
                        resultMsg += $"\n⚠️ Có {duplicateWarnings.Count} trường hợp trùng tên bị bỏ qua/chưa xử lý:\n" + string.Join("\n", duplicateWarnings.Take(5));
                        if (duplicateWarnings.Count > 5) resultMsg += $"\n... và {duplicateWarnings.Count - 5} trường hợp khác.";
                        resultMsg += "\n\nVui lòng tự chọn lại hoặc nhập điểm thủ công cho các học sinh này.";
                    }
                    resultMsg += "\n\nVui lòng bấm 'Lưu Bảng Điểm' để lưu vào CSDL.";

                    MessageBox.Show(resultMsg, "Kết quả dán điểm", MessageBoxButton.OK, duplicateWarnings.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }

                Serilog.Log.Information("[BatchGrading] Parsed {Count} entries from clipboard", successCount);
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "BatchGrading: Paste failed");
                MessageBox.Show($"❌ Lỗi paste: {ex.Message}", "Lỗi");
            }
        }

        // ═══ F1.2: QUICK FEEDBACK ═══
        private void BtnQuickFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tagText)
            {
                // Lấy DataGrid từ UI tree
                var dg = this.FindName("dgGrades") as DataGrid;
                if (dg?.SelectedItem == null)
                {
                    MessageBox.Show("⚠ Hãy chọn một (hoặc nhiều) học sinh trong bảng điểm trước khi gắn nhận xét.", "Chưa chọn học sinh", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int updateCount = 0;
                foreach (var selectedItem in dg.SelectedItems)
                {
                    if (selectedItem is System.Data.DataRowView rowView)
                    {
                        string currentNote = rowView["Note"]?.ToString() ?? "";
                        if (!currentNote.Contains(tagText))
                        {
                            string newNote = string.IsNullOrEmpty(currentNote) ? tagText : $"{currentNote}; {tagText}";
                            rowView["Note"] = newNote;
                            updateCount++;
                        }
                    }
                }

                // Force DataGrid refresh
                dg.Items.Refresh();

                if (updateCount > 0)
                {
                    // Có thể show toast nhẹ thay vì MessageBox
                    Serilog.Log.Information($"[QuickFeedback] Đã thêm '{tagText}' cho {updateCount} học sinh.");
                }
            }
        }

        // ═══ F1.3: SOFT SKILL RUBRIC ═══
        private void BtnSoftSkill_Click(object sender, RoutedEventArgs e)
        {
            var dg = this.FindName("dgGrades") as DataGrid;
            if (dg?.SelectedItem == null)
            {
                MessageBox.Show("⚠ Hãy chọn một học sinh trong bảng điểm để đánh giá kỹ năng mềm.", "Chưa chọn học sinh", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = dg.SelectedItem;
            string name = "Học sinh";
            int studentId = 0;

            if (selectedItem is System.Data.DataRowView rowView)
            {
                if (rowView.Row.Table.Columns.Contains("Họ và tên"))
                    name = rowView["Họ và tên"]?.ToString() ?? "Học sinh";
                else if (rowView.Row.Table.Columns.Contains("StudentName"))
                    name = rowView["StudentName"]?.ToString() ?? "Học sinh";
                else if (rowView.Row.Table.Columns.Contains("FullName"))
                    name = rowView["FullName"]?.ToString() ?? "Học sinh";

                if (rowView.Row.Table.Columns.Contains("StudentId") && int.TryParse(rowView["StudentId"]?.ToString(), out int parsedId))
                    studentId = parsedId;
                else if (rowView.Row.Table.Columns.Contains("Id") && int.TryParse(rowView["Id"]?.ToString(), out int parsedId2))
                    studentId = parsedId2;
            }

            if (studentId <= 0)
            {
                MessageBox.Show("⚠ Không tìm thấy mã học sinh hợp lệ để chấm điểm kỹ năng mềm. Vui lòng tải lại dữ liệu lớp học!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
 
            var rubricWin = new SoftSkillRubricWindow(studentId, name);
            rubricWin.Owner = Window.GetWindow(this);
            rubricWin.ShowDialog();
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private void SaveState()
        {
            var vm = DataContext as QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel;
            if (vm?.GradingTable != null)
            {
                _undoStack.Push(vm.GradingTable.Copy());
                _redoStack.Clear();
            }
        }

        private void RegisterDataGridEvents()
        {
            var dg = this.FindName("dgGrades") as DataGrid;
            if (dg != null)
            {
                dg.BeginningEdit -= DgGrades_BeginningEdit;
                dg.BeginningEdit += DgGrades_BeginningEdit;
            }
        }

        private void DgGrades_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            SaveState();
        }

        private void TeacherGradingView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                if (e.Key == System.Windows.Input.Key.Z)
                {
                    PerformUndo();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Y)
                {
                    PerformRedo();
                    e.Handled = true;
                }
            }
        }

        private void PerformUndo()
        {
            var vm = DataContext as QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel;
            if (vm?.GradingTable != null && _undoStack.Count > 0)
            {
                _redoStack.Push(vm.GradingTable.Copy());
                var previousTable = _undoStack.Pop();
                
                vm.GradingTable.BeginLoadData();
                vm.GradingTable.Clear();
                foreach (System.Data.DataRow r in previousTable.Rows)
                {
                    vm.GradingTable.ImportRow(r);
                }
                vm.GradingTable.EndLoadData();
                vm.GradingTable.AcceptChanges();
                
                var dg = this.FindName("dgGrades") as DataGrid;
                dg?.Items.Refresh();
            }
        }

        private void PerformRedo()
        {
            var vm = DataContext as QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel;
            if (vm?.GradingTable != null && _redoStack.Count > 0)
            {
                _undoStack.Push(vm.GradingTable.Copy());
                var nextTable = _redoStack.Pop();
                
                vm.GradingTable.BeginLoadData();
                vm.GradingTable.Clear();
                foreach (System.Data.DataRow r in nextTable.Rows)
                {
                    vm.GradingTable.ImportRow(r);
                }
                vm.GradingTable.EndLoadData();
                vm.GradingTable.AcceptChanges();
                
                var dg = this.FindName("dgGrades") as DataGrid;
                dg?.Items.Refresh();
            }
        }
    }

    
    public class DuplicateStudentSelectorWindow : Window
    {
        public int SelectedRowIndex { get; private set; } = -1;
        public bool IsCancelled { get; private set; } = true;

        public DuplicateStudentSelectorWindow(string name, System.Collections.Generic.List<dynamic> list)
        {
            Title = $"Xử lý trùng tên: {name}";
            Width = 450;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI");
            ResizeMode = ResizeMode.NoResize;

            var sp = new StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new TextBlock 
            { 
                Text = $"Phát hiện nhiều học sinh tên \"{name}\".\nVui lòng chọn đúng học sinh để nhập điểm:", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            });

            var lb = new ListBox 
            { 
                Height = 180, 
                Margin = new Thickness(0, 0, 0, 15),
                DisplayMemberPath = "DisplayText",
                SelectedValuePath = "Id"
            };
            
            lb.ItemsSource = list;
            if (list.Count > 0) lb.SelectedIndex = 0;
            sp.Children.Add(lb);

            var buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var btnSelect = new Button 
            { 
                Content = "Chọn học sinh", 
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), 
                Foreground = Brushes.White, 
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 5, 0),
                FontWeight = FontWeights.Bold
            };
            btnSelect.Click += (s, e) =>
            {
                if (lb.SelectedValue != null)
                {
                    SelectedRowIndex = (int)lb.SelectedValue;
                    IsCancelled = false;
                    Close();
                }
            };
            Grid.SetColumn(btnSelect, 0);
            buttonGrid.Children.Add(btnSelect);

            var btnSkip = new Button 
            { 
                Content = "Bỏ qua dòng này", 
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")), 
                Foreground = Brushes.White, 
                Padding = new Thickness(10),
                Margin = new Thickness(5, 0, 0, 0),
                FontWeight = FontWeights.Bold
            };
            btnSkip.Click += (s, e) =>
            {
                IsCancelled = true;
                Close();
            };
            Grid.SetColumn(btnSkip, 1);
            buttonGrid.Children.Add(btnSkip);

            sp.Children.Add(buttonGrid);
            Content = sp;
        }
    }

}
