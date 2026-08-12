using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;

namespace QASmartClass.Leadership.Views
{
    public partial class SchoolEventCalendarView : Page
    {
        private AppDbContext? _db;

        public SchoolEventCalendarView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            dpStart.SelectedDate = DateTime.Now.Date;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            try { _db = new AppDbContext(); } catch { }
            LoadEvents();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void LoadEvents()
        {
            if (_db == null) return;
            var events = _db.SchoolEvents.OrderByDescending(e => e.StartTime).Take(50).ToList();
            lvEvents.ItemsSource = events;
        }

        private async void BtnSaveEvent_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                string title = txtTitle.Text.Trim();
                string location = txtLocation.Text.Trim();
                string desc = txtDescription.Text.Trim();
                string organizer = txtOrganizer.Text.Trim();

                string startT = txtStartTime.Text.Trim();
                string endT = txtEndTime.Text.Trim();

                if (!System.Text.RegularExpressions.Regex.IsMatch(startT, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$") ||
                    !System.Text.RegularExpressions.Regex.IsMatch(endT, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    MessageBox.Show("Giờ bắt đầu và kết thúc phải có dạng HH:mm (ví dụ: 08:00, 17:00).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime start = (dpStart.SelectedDate ?? DateTime.Now.Date).Add(TimeSpan.Parse(startT));
                DateTime end = (dpEnd.SelectedDate ?? start.Date).Add(TimeSpan.Parse(endT));

                if (end <= start)
                {
                    MessageBox.Show("Thời gian kết thúc phải sau thời gian bắt đầu.", "Lỗi logic thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("Vui lòng nhập Tên sự kiện.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(organizer))
                {
                    organizer = "BGH";
                }

                if (_db != null)
                {
                    var evt = new SchoolEvent
                    {
                        Title = title,
                        Location = location,
                        Description = desc,
                        StartTime = start,
                        EndTime = end,
                        Organizer = organizer,
                        Department = "All",
                        Status = "Planned",
                        CreatedAt = DateTime.Now
                    };

                    _db.SchoolEvents.Add(evt);
                    await _db.SaveChangesAsync();
                    
                    MessageBox.Show("Đã lưu sự kiện thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    txtTitle.Text = "";
                    txtLocation.Text = "";
                    txtDescription.Text = "";
                    txtOrganizer.Text = "";
                    txtStartTime.Text = "08:00";
                    txtEndTime.Text = "17:00";
                    dpEnd.SelectedDate = null;
                    LoadEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu sự kiện: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var events = lvEvents.ItemsSource as System.Collections.Generic.List<SchoolEvent>;
            if (events == null || !events.Any())
            {
                MessageBox.Show("Không có dữ liệu sự kiện để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel CSV Files (*.csv)|*.csv",
                FileName = "Lich_Cong_Tac_Tuan_" + DateTime.Now.ToString("yyyyMMdd")
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    using (var stream = new System.IO.FileStream(saveDlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        // Write UTF-8 BOM so Microsoft Excel can read Vietnamese characters correctly
                        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
                        stream.Write(bom, 0, bom.Length);

                        using (var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8))
                        {
                            // Write CSV Headers
                            writer.WriteLine("Tiêu đề sự kiện,Bắt đầu,Kết thúc,Người tổ chức,Địa điểm,Nội dung chi tiết");

                            foreach (var ev in events)
                            {
                                string title = EscapeCsv(ev.Title);
                                string start = ev.StartTime.ToString("dd/MM/yyyy HH:mm");
                                string end = ev.EndTime.ToString("dd/MM/yyyy HH:mm");
                                string organizer = EscapeCsv(ev.Organizer);
                                string location = EscapeCsv(ev.Location);
                                string desc = EscapeCsv(ev.Description);

                                writer.WriteLine($"{title},{start},{end},{organizer},{location},{desc}");
                            }
                        }
                    }
                    MessageBox.Show("Xuất tệp Excel (CSV) thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Có lỗi xảy ra khi xuất dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var events = lvEvents.ItemsSource as System.Collections.Generic.List<SchoolEvent>;
            if (events == null || !events.Any())
            {
                MessageBox.Show("Không có dữ liệu sự kiện để in.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create a FlowDocument dynamically for professional printing layout
                var doc = new System.Windows.Documents.FlowDocument
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    PagePadding = new Thickness(40),
                    ColumnWidth = double.PositiveInfinity // Single column layout
                };

                // Add Administrative header
                var headerTable = new System.Windows.Documents.Table { Margin = new Thickness(0, 0, 0, 20) };
                headerTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(250) });
                headerTable.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(1, GridUnitType.Star) });
                
                var headerRowGroup = new System.Windows.Documents.TableRowGroup();
                var headerRow = new System.Windows.Documents.TableRow();
                
                var cellLeft = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("SỞ GD&ĐT TRƯỜNG THPT SỐ HÓA\nQA SMART SCHOOL") 
                    { FontSize = 10, FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Left };
                
                var cellRight = new System.Windows.Documents.TableCell(new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nĐộc lập - Tự do - Hạnh phúc") 
                    { FontSize = 10, FontWeight = FontWeights.Bold })) { TextAlignment = TextAlignment.Right };
                
                headerRow.Cells.Add(cellLeft);
                headerRow.Cells.Add(cellRight);
                headerRowGroup.Rows.Add(headerRow);
                headerTable.RowGroups.Add(headerRowGroup);
                doc.Blocks.Add(headerTable);

                // Add Title paragraph
                var titlePara = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("LỊCH CÔNG TÁC BAN GIÁM HIỆU\n") { FontSize = 18, FontWeight = FontWeights.Bold }
                )
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 20)
                };
                titlePara.Inlines.Add(new System.Windows.Documents.Run("Tuần công tác hiện tại (Cập nhật ngày: " + DateTime.Now.ToString("dd/MM/yyyy") + ")") { FontSize = 12, FontStyle = FontStyles.Italic });
                doc.Blocks.Add(titlePara);

                // Add Grid Table for events
                var table = new System.Windows.Documents.Table 
                { 
                    CellSpacing = 0,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1, 1, 0, 0)
                };
                
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(150) }); // Time
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(180) }); // Location & Event
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Description

                var trg = new System.Windows.Documents.TableRowGroup();

                // Header row
                var headerRowTable = new System.Windows.Documents.TableRow { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")) };
                headerRowTable.Cells.Add(CreateTableCell("THỜI GIAN", true));
                headerRowTable.Cells.Add(CreateTableCell("SỰ KIỆN / ĐỊA ĐIỂM", true));
                headerRowTable.Cells.Add(CreateTableCell("NỘI DUNG CHI TIẾT", true));
                trg.Rows.Add(headerRowTable);

                foreach (var ev in events.OrderBy(x => x.StartTime))
                {
                    var row = new System.Windows.Documents.TableRow();
                    
                    string timeText = $"{ev.StartTime:dd/MM/yyyy HH:mm}\nđến {ev.EndTime:dd/MM/yyyy HH:mm}";
                    row.Cells.Add(CreateTableCell(timeText, false));
                    
                    string eventLocation = $"{ev.Title}\n📍 Địa điểm: {ev.Location}\n👤 Tổ chức: {ev.Organizer}";
                    row.Cells.Add(CreateTableCell(eventLocation, false));
                    
                    row.Cells.Add(CreateTableCell(ev.Description ?? "", false));
                    
                    trg.Rows.Add(row);
                }

                table.RowGroups.Add(trg);
                doc.Blocks.Add(table);

                // Add Approved Sign-off block
                var footerPara = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("\n\nHIỆU TRƯỞNG DUYỆT BÁO CÁO\n") { FontSize = 12, FontWeight = FontWeights.Bold }
                )
                {
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 30, 40, 0)
                };
                footerPara.Inlines.Add(new System.Windows.Documents.Run("(Ký tên, đóng dấu)") { FontSize = 10, FontStyle = FontStyles.Italic });
                doc.Blocks.Add(footerPara);

                // Trigger WPF Standard PrintDialog
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    doc.PageHeight = printDlg.PrintableAreaHeight;
                    doc.PageWidth = printDlg.PrintableAreaWidth;
                    
                    System.Windows.Documents.IDocumentPaginatorSource idps = doc;
                    printDlg.PrintDocument(idps.DocumentPaginator, "Lich_Cong_Tac_BGH");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi chuẩn bị tài liệu in: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Windows.Documents.TableCell CreateTableCell(string text, bool isHeader)
        {
            var cell = new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(text))
                {
                    Margin = new Thickness(6),
                    FontSize = isHeader ? 11 : 10,
                    FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal
                }
            )
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 1, 1)
            };
            return cell;
        }
    }
}
