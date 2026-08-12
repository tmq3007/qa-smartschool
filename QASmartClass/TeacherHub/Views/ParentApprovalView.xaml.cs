using QASmartClass.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class ParentApprovalView : UserControl
    {
        private AppDbContext? _db;

        public ParentApprovalView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            LoadData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        // DÃ²ng 21-25 cÅ© (OnLoaded) Ä‘Æ°á»£c gá»¡ bá» vÃ¬ logic Ä‘Ã£ chuyá»ƒn sang Page_Loaded

        private void LoadData()
        {
            if (_db == null) return;
            try
            {
                var query = _db.ParentApprovals.AsQueryable();

                string filter = (CboStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Táº¥t cáº£";
                if (filter != "Táº¥t cáº£")
                {
                    query = query.Where(a => a.Status == filter);
                }

                var list = query.OrderByDescending(a => a.Id).ToList().Select(a => new
                {
                    a.Id,
                    a.StudentId,
                    a.DocumentType,
                    DocumentContent = "KÃ­nh gá»­i Phá»¥ huynh...",
                    StatusText = a.Status,
                    StatusBg = a.Status == "Signed" ? "#DCFCE7" : (a.Status == "Pending" ? "#FEF9C3" : "#FEE2E2"),
                    StatusFg = a.Status == "Signed" ? "#16A34A" : (a.Status == "Pending" ? "#CA8A04" : "#DC2626"),
                    SignedAtText = a.Status == "Signed" ? a.SignedAt.ToString("dd/MM/yyyy HH:mm") : "Chua ky"
                }).ToList();

                DgApprovals.ItemsSource = list;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[ParentApproval] Load error");
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void BtnSendRequest_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                var dlg = new Window
                {
                    Title = "Gá»­i YÃªu Cáº§u Chá»¯ KÃ½ XÃ¡c Nháº­n Sá»• LiÃªn Láº¡c", Width = 400, Height = 220,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), ResizeMode = ResizeMode.NoResize
                };
                var sp = new StackPanel { Margin = new Thickness(20) };

                var cboStudent = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 15), Height = 32 };
                var activeStudents = _db.Students.Where(s => s.Status == "Active").ToList();
                cboStudent.ItemsSource = activeStudents;
                cboStudent.DisplayMemberPath = "FullName";
                cboStudent.SelectedValuePath = "Id";
                if (activeStudents.Any()) cboStudent.SelectedIndex = 0;
                sp.Children.Add(new TextBlock { Text = "Chá»n há»c sinh:", Margin = new Thickness(0, 0, 0, 4), FontWeight = FontWeights.SemiBold });
                sp.Children.Add(cboStudent);

                var btn = new Button
                {
                    Content = "Gá»­i YÃªu Cáº§u", Height = 36, FontWeight = FontWeights.Bold,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                    Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
                };
                btn.Click += (_, __) =>
                {
                    try
                    {
                        if (cboStudent.SelectedValue == null) return;
                        int studentId = (int)cboStudent.SelectedValue;
                        var student = _db.Students.Find(studentId);
                        if (student == null) return;

                        var currentContactBook = _db.ContactBookEntries
                            .Where(c => c.StudentId == studentId)
                            .OrderByDescending(c => c.UpdatedAt)
                            .FirstOrDefault();

                        if (currentContactBook == null)
                        {
                            currentContactBook = new ContactBookEntry
                            {
                                StudentId = studentId,
                                StudentName = student.FullName,
                                SchoolYear = "2025-2026",
                                Semester = "HK2",
                                AverageGrade = 8.0,
                                AbsentDays = 0,
                                Conduct = "Tá»‘t",
                                TeacherComment = "Há»c sinh ngoan, tiáº¿n bá»™.",
                                UpdatedAt = DateTime.Now
                            };
                            _db.ContactBookEntries.Add(currentContactBook);
                            _db.SaveChanges();
                        }

                        var req = new ParentApproval
                        {
                            StudentId = studentId,
                            DocumentType = "Sá»• LiÃªn Láº¡c",
                            ReferenceId = currentContactBook.Id,
                            Status = "Pending"
                        };
                        
                        _db.ParentApprovals.Add(req);
                        _db.SaveChanges();

                        string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Teacher";
                        QASmartClass.Services.AuditHelper.Log(_db, "Request_Parent_Signature", actor, $"Requested signature for Student {student.FullName}");
                        
                        MessageBox.Show($"ÄÃ£ gá»­i yÃªu cáº§u kÃ½ xÃ¡c nháº­n sá»• liÃªn láº¡c cá»§a há»c sinh {student.FullName} tá»›i phá»¥ huynh!", "ThÃ nh cÃ´ng", MessageBoxButton.OK, MessageBoxImage.Information);
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                };
                sp.Children.Add(btn);
                dlg.Content = sp;
                dlg.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnSimulateSign_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                var req = _db.ParentApprovals.Find(id);
                if (req != null && req.Status == "Pending")
                {
                    req.Status = "Signed";
                    req.SignedAt = DateTime.Now;
                    req.SignatureToken = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    
                    _db.SaveChanges();
                    QASmartClass.Services.AuditHelper.Log(_db, "Parent_Signed", "System", $"Parent signed document {id} via OTP");
                    LoadData();
                    MessageBox.Show($"á»ž Phá»¥ huynh Ä‘Ã£ kÃ½ thÃ nh cÃ´ng (MÃ£ xÃ¡c thá»±c OTP: {req.SignatureToken})", "MÃ´ phá»ng Phá»¥ huynh");
                }
                else if (req != null)
                {
                    MessageBox.Show("YÃªu cáº§u nÃ y Ä‘Ã£ Ä‘Æ°á»£c xá»­ lÃ½ rá»“i.");
                }
            }
        }
    }
}

